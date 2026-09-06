import { useQuery } from '@tanstack/react-query';
import { clubsApi } from '../services/api';
import { useAuth } from '../hooks/useAuth';

export function DashboardPage() {
  const { user } = useAuth();
  const clubs = useQuery({ queryKey: ['clubs'], queryFn: clubsApi.list });
  const clubId = clubs.data?.[0]?.id;
  const dashboard = useQuery({
    queryKey: ['dashboard', clubId],
    queryFn: () => clubsApi.dashboard(clubId!),
    enabled: !!clubId
  });

  const d = dashboard.data;
  const cards = d
    ? [
        ['Canchas', d.courtCount],
        ['Camaras online', d.camerasOnline],
        ['Offline', d.camerasOffline],
        ['Partidos hoy', d.matchesToday],
        ['Videos', d.videosGenerated],
        ['Clips vendidos', d.clipsSold],
        ['Ingresos', `$${d.revenue}`],
        ['Pagos pendientes', d.pendingPayments]
      ]
    : [];

  return (
    <div className="space-y-6 pb-16">
      <div>
        <p className="text-sm text-accent">Hola {user?.firstName}</p>
        <h1 className="font-display text-3xl">Panel del club</h1>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map(([label, value]) => (
          <div key={String(label)} className="card">
            <p className="text-xs uppercase tracking-wide text-slate-400">{label}</p>
            <p className="mt-2 font-display text-3xl">{value}</p>
          </div>
        ))}
      </div>
      {d?.cameraErrors?.length ? (
        <div className="card">
          <h2 className="mb-3 font-semibold">Errores de camaras</h2>
          {d.cameraErrors.map((e) => (
            <p key={e.cameraId + e.occurredAt} className="text-sm text-danger">
              {e.cameraName}: {e.message}
            </p>
          ))}
        </div>
      ) : null}
    </div>
  );
}
