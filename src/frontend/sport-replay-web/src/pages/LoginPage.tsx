import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { homePath } from '../utils/roles';

const schema = z.object({
  email: z.string().email(),
  password: z.string().min(8)
});

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const { register, handleSubmit, formState } = useForm({ resolver: zodResolver(schema) });

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4">
      <p className="font-display text-4xl text-accent">SPORTREPLAY</p>
      <h1 className="mt-2 text-2xl font-bold">Ingresar</h1>
      <form
        className="mt-6 space-y-4"
        onSubmit={handleSubmit(async (values) => {
          const auth = await login(values.email, values.password);
          navigate(homePath(auth.role));
        })}
      >
        <input className="input" placeholder="Email" {...register('email')} />
        <input className="input" type="password" placeholder="Password" {...register('password')} />
        {formState.errors.email && <p className="text-sm text-danger">Email invalido</p>}
        <button className="btn-primary w-full" disabled={formState.isSubmitting}>
          Ingresar
        </button>
      </form>
      <p className="mt-4 text-sm text-slate-400">
        Demo jugador: player@sportreplay.local / Player123!
      </p>
      <Link className="mt-2 text-accent" to="/register">
        Crear cuenta
      </Link>
    </div>
  );
}
