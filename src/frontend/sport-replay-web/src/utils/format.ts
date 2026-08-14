export const formatMoney = (value: number, currency = 'ARS') =>
  new Intl.NumberFormat('es-AR', { style: 'currency', currency }).format(value);
