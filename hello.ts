// Función HTTP mínima; el chequeo /health/neon espera exactamente este texto como respuesta.
export default async function hello(): Promise<Response> {
  return new Response("Hello from Neon Functions");
}
