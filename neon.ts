import { defineConfig } from "@neon/config/v1";

// Declara los servicios administrados de Neon asociados a este repositorio.
export default defineConfig({
  // Auth se habilita en Neon, pero el portal conserva su autenticación local con cookies.
  auth: true,
  functions: {
    // Endpoint mínimo usado por el chequeo de salud para verificar la función desplegada.
    hello: { name: "Hello World", source: "./hello.ts" },
  },
  // Branch policy: per-branch tuning
  branch: (branch) => {
    if (branch.isDefault) {
      // Default branch: no overrides, uses project defaults
      return {};
    }
    if (!branch.exists) {
      // New non-default branches: auto-expire
      // Run `neon checkout <name>` to create a new branch with these settings
      return { ttl: "7d" };
    }
    // Existing branch: no changes
    return {};
  },
});
