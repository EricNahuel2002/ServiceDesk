import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import {
  BarChart, Bar, LineChart, Line, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts'
import { Card } from '../../../components/common/Card'
import { Select } from '../../../components/common/Select'
import { AdminAppShell } from '../../../components/layout/AdminAppShell'
import { useTechnicians, useMetrics } from '../../../features/admin/queries'
import { requireAdmin } from '../../../features/admin/auth'
import { getPriorityLabel } from '../../../utils/priority'

export const Route = createFileRoute('/admin/metrics/')({
  beforeLoad: () => requireAdmin(),
  component: AdminMetricsPage,
})

const PIE_COLORS = ['#10B981', '#F59E0B', '#3B82F6', '#EF4444', '#6B7280']

const presets = [
  { label: 'Últimos 7 días', from: daysAgo(7), to: today() },
  { label: 'Últimos 30 días', from: daysAgo(30), to: today() },
  { label: 'Este mes', from: monthStart(), to: today() },
  { label: 'Mes pasado', from: lastMonthStart(), to: lastMonthEnd() },
  { label: 'Todo', from: undefined, to: undefined },
]

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

function daysAgo(n: number): string {
  const d = new Date()
  d.setDate(d.getDate() - n)
  return d.toISOString().slice(0, 10)
}

function monthStart(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`
}

function lastMonthStart(): string {
  const d = new Date()
  d.setMonth(d.getMonth() - 1)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`
}

function lastMonthEnd(): string {
  const d = new Date()
  d.setDate(0)
  return d.toISOString().slice(0, 10)
}

function AdminMetricsPage() {
  const technicians = useTechnicians()
  const [presetIdx, setPresetIdx] = useState(0)
  const [technicianId, setTechnicianId] = useState<string>('')

  const preset = presets[presetIdx]
  const params = {
    from: preset.from,
    to: preset.to,
    technicianId: technicianId || undefined,
  }

  const metrics = useMetrics(params)

  const data = metrics.data

  const priorityChartData = data
    ? data.byPriority.map((p) => ({
        name: getPriorityLabel(p.priority),
        total: p.count,
        overdue: p.overdueCount,
      }))
    : []

  const dailyChartData = data
    ? data.dailyTrend.map((d) => ({
        date: d.date,
        Creados: d.created,
        Resueltos: d.resolved,
      }))
    : []

  const techChartData = data
    ? data.byTechnician.map((t) => ({
        name: `${t.firstName} ${t.lastName}`,
        Asignados: t.assignedCount,
        Resueltos: t.resolvedCount,
      }))
    : []

  return (
    <AdminAppShell>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Métricas</h1>
      </div>

      <div className="mb-6 flex flex-wrap gap-4">
        <div className="flex gap-1 rounded-lg border border-gray-200 bg-gray-100 p-1">
          {presets.map((p, i) => (
            <button
              key={p.label}
              type="button"
              onClick={() => setPresetIdx(i)}
              className={`rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                presetIdx === i
                  ? 'bg-[#0F52BA] text-white'
                  : 'text-gray-600 hover:bg-gray-200'
              }`}
            >
              {p.label}
            </button>
          ))}
        </div>

        <div className="min-w-[220px]">
          <Select
            value={technicianId}
            onChange={(e) => setTechnicianId(e.target.value)}
          >
            <option value="">Todos los técnicos</option>
            {(technicians.data ?? []).map((t) => (
              <option key={t.id} value={t.id}>
                {t.firstName} {t.lastName}
              </option>
            ))}
          </Select>
        </div>
      </div>

      {metrics.isPending ? (
        <p className="text-gray-500">Cargando métricas...</p>
      ) : !data ? (
        <p className="text-gray-500">No se pudieron cargar las métricas.</p>
      ) : (
        <>
          <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Card>
              <p className="text-sm text-gray-500">Total</p>
              <p className="mt-1 text-2xl font-semibold text-gray-900">{data.totalTickets}</p>
            </Card>
            <Card>
              <p className="text-sm text-gray-500">Abiertos</p>
              <p className="mt-1 text-2xl font-semibold text-[#0F52BA]">{data.openTickets}</p>
            </Card>
            <Card>
              <p className="text-sm text-gray-500">En progreso</p>
              <p className="mt-1 text-2xl font-semibold text-amber-600">{data.inProgressTickets}</p>
            </Card>
            <Card>
              <p className="text-sm text-gray-500">Resueltos</p>
              <p className="mt-1 text-2xl font-semibold text-green-600">{data.resolvedTickets}</p>
            </Card>
          </div>

          <div className="mb-6 grid gap-4 sm:grid-cols-3">
            <Card>
              <p className="text-sm text-gray-500">Cumplimiento SLA</p>
              <p className="mt-1 text-2xl font-semibold text-green-600">
                {data.slaCompliancePercentage}%
              </p>
            </Card>
            <Card>
              <p className="text-sm text-gray-500">Ticketes retrasados</p>
              <p className="mt-1 text-2xl font-semibold text-red-600">{data.overdueTickets}</p>
            </Card>
            <Card>
              <p className="text-sm text-gray-500">Tiempo promedio de resolución</p>
              <p className="mt-1 text-2xl font-semibold text-gray-900">
                {data.averageResolutionHours}h
              </p>
            </Card>
          </div>

          <div className="mb-6 grid gap-6 lg:grid-cols-2">
            <Card>
              <h3 className="mb-4 text-sm font-semibold text-gray-900">Tendencia diaria</h3>
              {dailyChartData.length > 0 ? (
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={dailyChartData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                    <YAxis allowDecimals={false} />
                    <Tooltip />
                    <Legend />
                    <Line type="monotone" dataKey="Creados" stroke="#3B82F6" strokeWidth={2} />
                    <Line type="monotone" dataKey="Resueltos" stroke="#10B981" strokeWidth={2} />
                  </LineChart>
                </ResponsiveContainer>
              ) : (
                <p className="py-8 text-center text-sm text-gray-400">Sin datos</p>
              )}
            </Card>

            <Card>
              <h3 className="mb-4 text-sm font-semibold text-gray-900">Por prioridad</h3>
              {priorityChartData.length > 0 ? (
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={priorityChartData}
                      dataKey="total"
                      nameKey="name"
                      cx="50%"
                      cy="50%"
                      outerRadius={100}
                      label={({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`}
                    >
                      {priorityChartData.map((_, i) => (
                        <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              ) : (
                <p className="py-8 text-center text-sm text-gray-400">Sin datos</p>
              )}
            </Card>
          </div>

          {techChartData.length > 0 && (
            <Card className="mb-6">
              <h3 className="mb-4 text-sm font-semibold text-gray-900">Por técnico</h3>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={techChartData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey="Asignados" fill="#3B82F6" />
                  <Bar dataKey="Resueltos" fill="#10B981" />
                </BarChart>
              </ResponsiveContainer>
            </Card>
          )}

          {data.byTechnician.length > 0 && (
            <Card>
              <h3 className="mb-4 text-sm font-semibold text-gray-900">Detalle por técnico</h3>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 text-left text-xs uppercase tracking-wide text-gray-500">
                      <th className="pb-2 font-medium">Técnico</th>
                      <th className="pb-2 font-medium">Asignados</th>
                      <th className="pb-2 font-medium">Resueltos</th>
                      <th className="pb-2 font-medium">Promedio resolución</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.byTechnician.map((t) => (
                      <tr key={t.userId} className="border-b border-gray-100">
                        <td className="py-2 font-medium text-gray-900">
                          {t.firstName} {t.lastName}
                        </td>
                        <td className="py-2 text-gray-700">{t.assignedCount}</td>
                        <td className="py-2 text-gray-700">{t.resolvedCount}</td>
                        <td className="py-2 text-gray-700">
                          {t.averageResolutionHours > 0 ? `${t.averageResolutionHours}h` : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          )}
        </>
      )}
    </AdminAppShell>
  )
}
