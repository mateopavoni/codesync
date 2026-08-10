import { Injectable } from '@angular/core';

// Carga Monaco Editor de forma lazy desde los assets (loader AMD).
// El singleton garantiza que Monaco se inicializa una sola vez aunque
// haya múltiples instancias del editor montadas.
@Injectable({ providedIn: 'root' })
export class MonacoLoaderService {
  private loadPromise: Promise<void> | null = null;

  load(): Promise<void> {
    if (this.loadPromise) return this.loadPromise;

    this.loadPromise = new Promise<void>((resolve, reject) => {
      if (window.monaco) {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = '/assets/monaco/vs/loader.js';
      script.async = true;
      script.onload = () => {
        // Path absoluto (con "/" inicial): los web workers de Monaco resuelven
        // sus propias URLs internas (p.ej. tsWorker.js) contra este valor, y una
        // ruta relativa falla al parsear dentro del contexto del worker.
        window.require.config({ paths: { vs: '/assets/monaco/vs' } });
        window.require(['vs/editor/editor.main'], () => resolve());
      };
      script.onerror = () => reject(new Error('No se pudo cargar Monaco Editor desde assets.'));
      document.head.appendChild(script);
    });

    return this.loadPromise;
  }
}
