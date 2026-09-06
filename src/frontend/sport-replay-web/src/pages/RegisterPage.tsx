import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { homePath } from '../utils/roles';

const schema = z.object({
  firstName: z.string().min(2),
  lastName: z.string().min(2),
  email: z.string().email(),
  password: z.string().min(8),
  role: z.enum(['Player', 'ClubOwner', 'Operator'])
});

type FormValues = z.infer<typeof schema>;

export function RegisterPage() {
  const { register: signup } = useAuth();
  const navigate = useNavigate();
  const { register, handleSubmit } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { firstName: '', lastName: '', email: '', password: '', role: 'Player' }
  });

  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-4">
      <h1 className="font-display text-3xl text-accent">Crear cuenta</h1>
      <form
        className="mt-6 space-y-3"
        onSubmit={handleSubmit(async (values) => {
          const auth = await signup(values);
          navigate(homePath(auth.role));
        })}
      >
        <input className="input" placeholder="Nombre" {...register('firstName')} />
        <input className="input" placeholder="Apellido" {...register('lastName')} />
        <input className="input" placeholder="Email" {...register('email')} />
        <input className="input" type="password" placeholder="Password" {...register('password')} />
        <select className="input" {...register('role')}>
          <option value="Player">Jugador</option>
          <option value="ClubOwner">Propietario</option>
          <option value="Operator">Operador</option>
        </select>
        <button className="btn-primary w-full">Registrarme</button>
      </form>
    </div>
  );
}
