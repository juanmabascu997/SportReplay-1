import type { Role } from '../types';

export function isStaff(role?: Role | string | null) {
  return role === 'Admin' || role === 'ClubOwner' || role === 'Operator';
}

export function homePath(role?: Role | string | null) {
  return isStaff(role) ? '/dashboard' : '/buscar';
}

export const ARGENTINA_PROVINCES = [
  'CABA',
  'Buenos Aires',
  'Catamarca',
  'Chaco',
  'Chubut',
  'Cordoba',
  'Corrientes',
  'Entre Rios',
  'Formosa',
  'Jujuy',
  'La Pampa',
  'La Rioja',
  'Mendoza',
  'Misiones',
  'Neuquen',
  'Rio Negro',
  'Salta',
  'San Juan',
  'San Luis',
  'Santa Cruz',
  'Santa Fe',
  'Santiago del Estero',
  'Tierra del Fuego',
  'Tucuman'
];
