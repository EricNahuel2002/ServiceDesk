import {
  HubConnectionBuilder,
  HubConnection,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import type { ChatMessageDto } from './types'

const HUB_URL: string = import.meta.env.VITE_API_BASE_URL
  ? `${import.meta.env.VITE_API_BASE_URL.replace('/api', '')}/hubs/chat`
  : '/hubs/chat'

type MessageCallback = (message: ChatMessageDto) => void
type TypingCallback = (data: { userId: string; firstName: string; lastName: string; ticketId: string }) => void
type PresenceCallback = (data: { userId: string }) => void
type TicketPresenceCallback = (data: { userId: string; firstName: string; ticketId: string }) => void

class ChatConnectionManager {
  private connection: HubConnection | null = null
  private messageCallbacks: Map<string, Set<MessageCallback>> = new Map()
  private typingCallbacks: Map<string, Set<TypingCallback>> = new Map()
  private userJoinedCallbacks: Set<TicketPresenceCallback> = new Set()
  private userLeftCallbacks: Set<TicketPresenceCallback> = new Set()
  private userConnectedCallbacks: Set<PresenceCallback> = new Set()
  private userDisconnectedCallbacks: Set<PresenceCallback> = new Set()
  private joinedTickets: Set<string> = new Set()
  private readyPromise: Promise<void> | null = null
  private readyResolve: (() => void) | null = null
  private readyReject: ((error: Error) => void) | null = null
  private reconnectedCallbacks: Set<() => void> = new Set()

  private waitForReady(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return Promise.resolve()
    }
    if (this.readyPromise) {
      return this.readyPromise
    }
    return Promise.reject(new Error('Connection not started'))
  }

  async start(): Promise<void> {
    if (this.connection && this.connection.state !== HubConnectionState.Disconnected) {
      await this.waitForReady()
      return
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => {
          const raw = localStorage.getItem('servicedesk.auth')
          if (!raw) return ''
          try {
            const session = JSON.parse(raw)
            return session.accessToken ?? ''
          } catch {
            return ''
          }
        },
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.elapsedMilliseconds > 60_000) {
            return null
          }
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30_000)
        },
      })
      .configureLogging(LogLevel.Warning)
      .build()

    this.readyPromise = new Promise<void>((resolve, reject) => {
      this.readyResolve = resolve
      this.readyReject = reject
    })

    this.connection.on('ReceiveMessage', (message: ChatMessageDto) => {
      const callbacks = this.messageCallbacks.get(message.ticketId)
      if (callbacks) {
        callbacks.forEach((cb) => cb(message))
      }
    })

    this.connection.on('UserTyping', (data: { userId: string; firstName: string; lastName: string; ticketId: string }) => {
      const callbacks = this.typingCallbacks.get(data.ticketId)
      if (callbacks) {
        callbacks.forEach((cb) => cb(data))
      }
    })

    this.connection.on('UserJoined', (data: { userId: string; firstName: string; ticketId: string }) => {
      this.userJoinedCallbacks.forEach((cb) => cb(data))
    })

    this.connection.on('UserLeft', (data: { userId: string; firstName: string; ticketId: string }) => {
      this.userLeftCallbacks.forEach((cb) => cb(data))
    })

    this.connection.on('UserConnected', (data: { userId: string }) => {
      this.userConnectedCallbacks.forEach((cb) => cb(data))
    })

    this.connection.on('UserDisconnected', (data: { userId: string }) => {
      this.userDisconnectedCallbacks.forEach((cb) => cb(data))
    })

    this.connection.onreconnected(async () => {
      console.log('[SignalR] Reconnected, rejoining groups')
      const ticketsToRejoin = [...this.joinedTickets]
      this.joinedTickets.clear()
      for (const ticketId of ticketsToRejoin) {
        try {
          await this.connection!.invoke('JoinTicket', ticketId)
          this.joinedTickets.add(ticketId)
        } catch (err) {
          console.error('[SignalR] Failed to rejoin ticket group', ticketId, err)
        }
      }
      this.reconnectedCallbacks.forEach((cb) => cb())
    })

    this.connection.onclose((error) => {
      console.warn('[SignalR] Connection closed', error)
      this.readyPromise = null
      this.readyResolve = null
      this.readyReject = null
      this.connection = null
      this.joinedTickets.clear()
    })

    try {
      await this.connection.start()
      console.log('[SignalR] Connected')
      this.readyResolve?.()
      this.readyResolve = null
      this.readyReject = null
    } catch (error) {
      console.error('[SignalR] Failed to start connection', error)
      this.readyReject?.(new Error('Failed to start SignalR connection'))
      this.readyPromise = null
      this.readyResolve = null
      this.readyReject = null
      this.connection = null
      throw error
    }
  }

  async stop(): Promise<void> {
    if (this.connection) {
      this.readyPromise = null
      this.readyResolve = null
      this.readyReject = null
      await this.connection.stop()
      this.connection = null
      this.joinedTickets.clear()
    }
  }

  async joinTicket(ticketId: string): Promise<void> {
    await this.waitForReady()
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      console.warn('[SignalR] joinTicket called but not connected')
      return
    }
    if (this.joinedTickets.has(ticketId)) {
      return
    }
    await this.connection.invoke('JoinTicket', ticketId)
    this.joinedTickets.add(ticketId)
  }

  async leaveTicket(ticketId: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return
    }
    await this.connection.invoke('LeaveTicket', ticketId)
    this.joinedTickets.delete(ticketId)
  }

  async sendMessage(ticketId: string, content: string): Promise<void> {
    await this.waitForReady()
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      console.warn('[SignalR] sendMessage called but not connected')
      return
    }
    await this.connection.invoke('SendMessage', ticketId, content)
  }

  async sendTyping(ticketId: string): Promise<void> {
    await this.waitForReady()
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return
    }
    await this.connection.invoke('SendTyping', ticketId)
  }

  onReceiveMessage(ticketId: string, callback: MessageCallback): () => void {
    if (!this.messageCallbacks.has(ticketId)) {
      this.messageCallbacks.set(ticketId, new Set())
    }
    this.messageCallbacks.get(ticketId)!.add(callback)
    return () => {
      this.messageCallbacks.get(ticketId)?.delete(callback)
    }
  }

  onUserTyping(ticketId: string, callback: TypingCallback): () => void {
    if (!this.typingCallbacks.has(ticketId)) {
      this.typingCallbacks.set(ticketId, new Set())
    }
    this.typingCallbacks.get(ticketId)!.add(callback)
    return () => {
      this.typingCallbacks.get(ticketId)?.delete(callback)
    }
  }

  onUserJoined(callback: TicketPresenceCallback): () => void {
    this.userJoinedCallbacks.add(callback)
    return () => { this.userJoinedCallbacks.delete(callback) }
  }

  onUserLeft(callback: TicketPresenceCallback): () => void {
    this.userLeftCallbacks.add(callback)
    return () => { this.userLeftCallbacks.delete(callback) }
  }

  onUserConnected(callback: PresenceCallback): () => void {
    this.userConnectedCallbacks.add(callback)
    return () => { this.userConnectedCallbacks.delete(callback) }
  }

  onUserDisconnected(callback: PresenceCallback): () => void {
    this.userDisconnectedCallbacks.add(callback)
    return () => { this.userDisconnectedCallbacks.delete(callback) }
  }

  onReconnected(callback: () => void): () => void {
    this.reconnectedCallbacks.add(callback)
    return () => { this.reconnectedCallbacks.delete(callback) }
  }

  get isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected
  }
}

export const chatConnection = new ChatConnectionManager()
