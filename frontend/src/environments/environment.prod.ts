export const environment = {
  production: true,
  // Vacío = rutas relativas; nginx.conf reenvía /api/* al servicio "api" del docker-compose.
  apiBaseUrl: ''
};
