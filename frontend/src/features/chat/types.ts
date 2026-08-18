export interface ChatMessageDto {
  id: string
  ticketId: string
  senderId: string
  senderFirstName: string
  senderLastName: string
  content: string
  sentAtUtc: string
}
