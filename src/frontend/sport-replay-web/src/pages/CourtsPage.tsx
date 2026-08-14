import { useQuery } from '@tanstack/react-query';
import { clubsApi } from '../services/api';

export function CourtsPage() {
  const clubs = useQuery({ queryKey: ['clubs'], queryFn: clubsApi.list });
  const clubId = clubs.data?.[0]?.id;
  const courts = useQuery({ queryKey: ['courts', clubId], queryFn: () => clubsApi.courts(clubId!), enabled: !!clubId });

  return (
    <div className="space-y-4 pb-16">
      <h1 className="font-display text-3xl">Canchas</h1>
      {courts.data?.map((court) => (
        <div key={court.id} className="card">
          <p className="text-lg font-semibold">{court.name}</p>
          <p className="text-sm text-slate-400">Club {clubs.data?.[0]?.name}</p>
        </div>
      ))}
    </div>
  );
}
