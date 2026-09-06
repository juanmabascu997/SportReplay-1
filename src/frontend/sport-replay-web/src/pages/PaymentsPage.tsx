import { useSearchParams } from 'react-router-dom';

export function PaymentsPage() {
  const [params] = useSearchParams();
  const status = params.get('status');
  return (
    <div className="card space-y-3">
      <h1 className="font-display text-3xl">Pagos</h1>
      <p>Los pagos se procesan con Mercado Pago Checkout Pro.</p>
      {status && <p className="text-accent">Estado de retorno: {status}</p>}
      <p className="text-sm text-slate-400">
        Cuando Mercado Pago confirma el pago, enviamos el video del partido a WhatsApp de forma automatica.
      </p>
    </div>
  );
}

export function ProfilePage() {
  return (
    <div className="card">
      <h1 className="font-display text-3xl">Perfil</h1>
      <p className="mt-2 text-slate-400">Gestiona tu cuenta y solicitudes de video.</p>
    </div>
  );
}

export function AdminPage() {
  return (
    <div className="space-y-4">
      <h1 className="font-display text-3xl">Admin</h1>
      <div className="card">
        <p>Usuarios, clubes, camaras y pagos globales.</p>
        <a className="btn-primary mt-4 inline-flex" href="/admin/cameras">Ver camaras</a>
      </div>
    </div>
  );
}
