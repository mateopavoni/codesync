// Declaraciones globales para APIs cargadas en runtime (Monaco AMD loader)

import type * as MonacoType from 'monaco-editor';

declare global {
  interface Window {
    monaco: typeof MonacoType;
    require: MonacoRequire;
    MonacoEnvironment?: {
      getWorkerUrl?: (workerId: string, label: string) => string;
      getWorker?: (workerId: string, label: string) => Worker;
    };
  }
}

interface MonacoRequire {
  config(opts: { paths: Record<string, string> }): void;
  (deps: string[], callback: (...args: unknown[]) => void): void;
}

export {};
