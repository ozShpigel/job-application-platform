import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api/match': 'http://localhost:5136',
      '/api/applications': 'http://localhost:5002',
    },
  },
});
