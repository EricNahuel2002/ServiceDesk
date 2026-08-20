import { createFileRoute } from '@tanstack/react-router'
import { useMemo, useState } from 'react'
import { Card } from '../../../components/common/Card'
import { Input } from '../../../components/common/Input'
import { Button } from '../../../components/common/Button'
import { AdminAppShell } from '../../../components/layout/AdminAppShell'
import {
  useSlaConfigurations,
  useUpdateSlaConfiguration,
  useBusinessHours,
  useUpdateBusinessHours,
} from '../../../features/admin/queries'
import { requireAdmin } from '../../../features/admin/auth'
import type { DaySchedule } from '../../../features/admin/types'

export const Route = createFileRoute('/admin/sla/')({
  beforeLoad: () => requireAdmin(),
  component: AdminSlaPage,
})

const PRIORITY_ORDER = [
  { value: 1, label: 'Baja' },
  { value: 2, label: 'Media' },
  { value: 3, label: 'Alta' },
  { value: 4, label: 'Crítica' },
] as const

const TIMEZONES = [
  'Argentina Standard Time',
  'SA Pacific Standard Time',
  'SA Eastern Standard Time',
  'US Eastern Standard Time',
  'US Pacific Standard Time',
  'UTC',
  'Romance Standard Time',
  'Central European Standard Time',
  'W. Europe Standard Time',
  'Tokyo Standard Time',
  'China Standard Time',
  'Singapore Standard Time',
]

const DAY_KEYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'] as const
const DAY_LABELS: Record<string, string> = {
  Monday: 'Lunes',
  Tuesday: 'Martes',
  Wednesday: 'Miércoles',
  Thursday: 'Jueves',
  Friday: 'Viernes',
  Saturday: 'Sábado',
  Sunday: 'Domingo',
}

function AdminSlaPage() {
  const slaQuery = useSlaConfigurations()
  const updateSla = useUpdateSlaConfiguration()
  const bhQuery = useBusinessHours()
  const updateBh = useUpdateBusinessHours()

  const [editedHours, setEditedHours] = useState<Record<number, string>>({})
  const [bhError, setBhError] = useState<string | null>(null)
  const [slaSaved, setSlaSaved] = useState(false)

  const [editedTimezone, setEditedTimezone] = useState<string | null>(null)
  const [editedUseBh, setEditedUseBh] = useState<boolean | null>(null)
  const [editedDays, setEditedDays] = useState<Record<string, DaySchedule>>({})
  const [editedMaxStart, setEditedMaxStart] = useState<string | null>(null)

  const serverHours = useMemo(() => {
    const map: Record<number, string> = {}
    slaQuery.data?.forEach((config) => {
      map[config.priority] = String(config.responseTimeHours)
    })
    return map
  }, [slaQuery.data])

  const serverBh = useMemo(() => {
    if (!bhQuery.data) {
      return { timezone: '', useBh: true, days: {} as Record<string, DaySchedule>, maxAssignmentToStartMinutes: 120 }
    }
    const days = (() => {
      try {
        const parsed = JSON.parse(bhQuery.data.businessHoursJson) as Record<
          string,
          Partial<DaySchedule> & { Enabled?: boolean; Start?: string | null; End?: string | null }
        >
        return Object.fromEntries(
          Object.entries(parsed).map(([day, value]) => [
            day,
            {
              enabled: value.enabled ?? value.Enabled ?? false,
              start: value.start ?? value.Start ?? null,
              end: value.end ?? value.End ?? null,
            },
          ]),
        )
      } catch {
        return {} as Record<string, DaySchedule>
      }
    })()
    return {
      timezone: bhQuery.data.timeZoneId,
      useBh: bhQuery.data.useBusinessHours,
      days,
      maxAssignmentToStartMinutes: bhQuery.data.maxAssignmentToStartMinutes,
    }
  }, [bhQuery.data])

  const timezone = editedTimezone ?? serverBh.timezone
  const applyBusinessHours = editedUseBh ?? serverBh.useBh
  const days = { ...serverBh.days, ...editedDays }
  const maxStart = editedMaxStart ?? String(serverBh.maxAssignmentToStartMinutes ?? 120)

  function handleHoursChange(priority: number, value: string) {
    setEditedHours((prev) => ({ ...prev, [priority]: value }))
    setSlaSaved(false)
  }

  function handleSaveSla(priority: number) {
    const value = editedHours[priority] ?? serverHours[priority] ?? '0'
    const parsed = parseInt(value, 10)
    if (isNaN(parsed) || parsed < 0) return
    setSlaSaved(false)
    updateSla.mutate(
      { priority, responseTimeHours: parsed },
      { onSuccess: () => setSlaSaved(true) },
    )
  }

  function handleDayChange(day: string, field: keyof DaySchedule, value: boolean | string) {
    setEditedDays((prev) => {
      const current = prev[day] ?? serverBh.days[day]
      return {
        ...prev,
        [day]: {
          enabled: current?.enabled ?? false,
          start: current?.start ?? '08:00',
          end: current?.end ?? '17:00',
          [field]: value,
        },
      }
    })
  }

  function handleSaveBusinessHours() {
    setBhError(null)
    updateBh.mutate(
      {
        businessHoursJson: JSON.stringify(days),
        timeZoneId: timezone,
        useBusinessHours: applyBusinessHours,
        maxAssignmentToStartMinutes: parseInt(maxStart, 10) || 0,
      },
      {
        onError: () => setBhError('Error al guardar los horarios.'),
      },
    )
  }

  if (slaQuery.isPending || bhQuery.isPending) {
    return (
      <AdminAppShell>
        <p className="text-gray-500">Cargando...</p>
      </AdminAppShell>
    )
  }

  return (
    <AdminAppShell>
      <div className="flex flex-col gap-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Configuración SLA</h1>
          <p className="mt-1 text-sm text-gray-500">
            Define los tiempos de respuesta y horarios de atención de la empresa.
          </p>
        </div>

        <Card>
          <h2 className="mb-4 text-lg font-semibold text-gray-900">Tiempo de respuesta por prioridad</h2>
          <p className="mb-4 text-sm text-gray-500">
            Cantidad máxima de horas para responder a un ticket según su prioridad.
          </p>
          <div className="flex flex-col gap-4">
            {PRIORITY_ORDER.map((p) => (
              <div key={p.value} className="flex items-end gap-4">
                <div className="w-32">
                  <span className="text-sm font-medium text-gray-700">{p.label}</span>
                </div>
                <Input
                  label="Horas"
                  type="number"
                  min={0}
                  value={editedHours[p.value] ?? serverHours[p.value] ?? ''}
                  onChange={(e) => handleHoursChange(p.value, e.target.value)}
                  className="w-24"
                />
                <Button
                  variant="secondary"
                  disabled={updateSla.isPending}
                  onClick={() => handleSaveSla(p.value)}
                >
                  Guardar
                </Button>
              </div>
            ))}
            {slaSaved && (
              <p className="text-sm text-green-600">Guardado correctamente.</p>
            )}
          </div>
        </Card>

        <Card>
          <h2 className="mb-4 text-lg font-semibold text-gray-900">Horarios de atención</h2>
          <p className="mb-4 text-sm text-gray-500">
            Define los horarios laborales para el cálculo de SLA.
          </p>

          <div className="flex flex-col gap-4">
            <div className="flex items-center gap-3">
              <input
                id="useBusinessHours"
                type="checkbox"
                checked={applyBusinessHours}
                onChange={(e) => setEditedUseBh(e.target.checked)}
                className="h-4 w-4 rounded border-gray-300"
              />
              <label htmlFor="useBusinessHours" className="text-sm font-medium text-gray-700">
                Usar horarios de atención para cálculo de SLA
              </label>
            </div>

             <div className="w-72">
               <label htmlFor="timezone" className="mb-1 block text-sm font-medium text-gray-700">
                 Zona horaria
               </label>
               <select
                 id="timezone"
                 value={timezone}
                 onChange={(e) => setEditedTimezone(e.target.value)}
                 className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
               >
                 {TIMEZONES.map((tz) => (
                   <option key={tz} value={tz}>
                     {tz}
                   </option>
                 ))}
               </select>
             </div>

             <div className="w-72">
               <label htmlFor="maxStart" className="mb-1 block text-sm font-medium text-gray-700">
                 Tiempo máximo para iniciar (minutos)
               </label>
               <input
                 id="maxStart"
                 type="number"
                 min={0}
                 value={maxStart}
                 onChange={(e) => setEditedMaxStart(e.target.value)}
                 className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
                 placeholder="120"
               />
               <p className="mt-1 text-xs text-gray-500">
                 Tiempo razonable desde que se asigna un ticket hasta que el técnico lo inicia.
                 Pasado este límite, se cuenta como retraso.
               </p>
             </div>

            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200">
                    <th className="py-2 text-left font-medium text-gray-500">Día</th>
                    <th className="py-2 text-center font-medium text-gray-500">Habilitado</th>
                    <th className="py-2 text-left font-medium text-gray-500">Inicio</th>
                    <th className="py-2 text-left font-medium text-gray-500">Fin</th>
                  </tr>
                </thead>
                <tbody>
                  {DAY_KEYS.map((day) => {
                    const schedule = days[day]
                    const enabled = schedule?.enabled ?? false
                    return (
                      <tr key={day} className="border-b border-gray-100">
                        <td className="py-2 font-medium text-gray-700">{DAY_LABELS[day]}</td>
                        <td className="py-2 text-center">
                          <input
                            type="checkbox"
                            checked={enabled}
                            onChange={(e) => handleDayChange(day, 'enabled', e.target.checked)}
                            className="h-4 w-4 rounded border-gray-300"
                          />
                        </td>
                        <td className="py-2">
                          <input
                            type="time"
                            value={schedule?.start ?? '08:00'}
                            disabled={!enabled}
                            onChange={(e) => handleDayChange(day, 'start', e.target.value)}
                            className="w-28 rounded-md border border-gray-300 px-2 py-1 text-sm disabled:opacity-50"
                          />
                        </td>
                        <td className="py-2">
                          <input
                            type="time"
                            value={schedule?.end ?? '17:00'}
                            disabled={!enabled}
                            onChange={(e) => handleDayChange(day, 'end', e.target.value)}
                            className="w-28 rounded-md border border-gray-300 px-2 py-1 text-sm disabled:opacity-50"
                          />
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>

            <div className="flex items-center gap-4">
              <Button
                disabled={updateBh.isPending}
                onClick={handleSaveBusinessHours}
              >
                {updateBh.isPending ? 'Guardando...' : 'Guardar horarios'}
              </Button>
              {updateBh.isSuccess && (
                <p className="text-sm text-green-600">Guardado correctamente.</p>
              )}
              {bhError && (
                <p className="text-sm text-red-600">{bhError}</p>
              )}
            </div>
          </div>
        </Card>
      </div>
    </AdminAppShell>
  )
}
