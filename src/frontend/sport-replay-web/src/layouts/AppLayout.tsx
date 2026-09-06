import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { isStaff } from '../utils/roles';

const staffLinks = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/matches', label: 'Partidos' },
  { to: '/clubs', label: 'Clubes' },
  { to: '/courts', label: 'Canchas' },
  { to: '/cameras', label: 'Camaras' },
  { to: '/payments', label: 'Pagos' },
  { to: '/admin', label: 'Admin' }
];

const playerLinks = [
  { to: '/buscar', label: 'Buscar' },
  { to: '/payments', label: 'Pagos' },
  { to: '/profile', label: 'Perfil' }
];

export function AppLayout() {
  const { user, logout } = useAuth();
  const links = isStaff(user?.role) ? staffLinks : playerLinks;
  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-line bg-ink/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <div>
            <p className="font-display text-2xl tracking-wide text-accent">SPORTREPLAY</p>
            <p className="text-xs text-slate-400">
              {isStaff(user?.role) ? 'Replays para clubes' : 'Encontra tu partido'}
            </p>
          </div>
          <nav className="hidden gap-4 md:flex">
            {links.map((l) => (
              <NavLink key={l.to} to={l.to} className={({ isActive }) => `text-sm ${isActive ? 'text-accent' : 'text-slate-300'}`}>
                {l.label}
              </NavLink>
            ))}
          </nav>
          <div className="flex items-center gap-3 text-sm">
            <NavLink to="/profile">{user?.firstName}</NavLink>
            <button className="btn-ghost" onClick={() => logout()}>
              Salir
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">
        <Outlet />
      </main>
      <nav className={`fixed bottom-0 left-0 right-0 grid border-t border-line bg-ink md:hidden ${links.length === 3 ? 'grid-cols-3' : 'grid-cols-4'}`}>
        {links.slice(0, 4).map((l) => (
          <NavLink key={l.to} to={l.to} className="py-3 text-center text-xs text-slate-300">
            {l.label}
          </NavLink>
        ))}
      </nav>
    </div>
  );
}
