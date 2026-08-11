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
        // El paths.vs de arriba solo lo conoce el AMD loader del hilo principal.
        // Cada worker (workerMain.js) arranca con su propio loader interno, sin
        // ese config, y al resolver módulos como tsWorker.js contra una URL base
        // inválida tira "Failed to parse URL". getWorkerUrl le da a Monaco la URL
        // absoluta de workerMain.js para instanciar el worker con self.location
        // ya seteado, y desde ahí resuelve sus propios submódulos relativos bien.
        window.MonacoEnvironment = {
          getWorkerUrl: () => '/assets/monaco/vs/base/worker/workerMain.js',
        };
        window.require(['vs/editor/editor.main'], () => resolve());
      };
      script.onerror = () => reject(new Error('No se pudo cargar Monaco Editor desde assets.'));
      document.head.appendChild(script);
    });

    return this.loadPromise;
  }
}
