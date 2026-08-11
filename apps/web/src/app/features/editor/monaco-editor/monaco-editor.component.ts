import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  signal,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import type * as MonacoTypes from 'monaco-editor';
import { MonacoLoaderService } from '../../../core/services/monaco-loader.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import type { CursorPosition } from '../../../core/models/room.model';
import type { ProgrammingLanguage } from '../../../core/models/challenge.model';

@Component({
  selector: 'app-monaco-editor',
  standalone: true,
  imports: [LoadingSpinnerComponent, ErrorMessageComponent],
  templateUrl: './monaco-editor.component.html',
  styleUrl: './monaco-editor.component.css',
})
export class MonacoEditorComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('container', { static: true }) containerRef!: ElementRef<HTMLDivElement>;

  @Input() value = '';
  @Input() language: ProgrammingLanguage = 'javascript';
  @Input() readonly = false;
  @Input() remoteCursors: CursorPosition[] = [];

  @Output() readonly valueChange = new EventEmitter<string>();
  @Output() readonly cursorChange = new EventEmitter<{ lineNumber: number; column: number }>();

  readonly loading = signal(true);
  readonly editorError = signal<string | null>(null);

  private editor: MonacoTypes.editor.IStandaloneCodeEditor | null = null;
  // Decoraciones de cursores remotos (una colección por usuario)
  private remoteDecorations = new Map<string, MonacoTypes.editor.IEditorDecorationsCollection>();

  private readonly monacoLoader = inject(MonacoLoaderService);

  async ngAfterViewInit(): Promise<void> {
    try {
      await this.monacoLoader.load();
      this.initEditor();
      this.loading.set(false);
    } catch {
      this.editorError.set('No se pudo inicializar el editor. Recargá la página.');
      this.loading.set(false);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.editor) return;

    if (changes['value'] && !changes['value'].firstChange) {
      const incoming = changes['value'].currentValue as string;
      if (incoming !== this.editor.getValue()) {
        // Evitar feedback loop: solo actualizar si el valor viene de afuera
        this.editor.setValue(incoming);
      }
    }

    if (changes['remoteCursors']) {
      this.renderRemoteCursors(changes['remoteCursors'].currentValue as CursorPosition[]);
    }

    if (changes['readonly']) {
      this.editor.updateOptions({ readOnly: changes['readonly'].currentValue as boolean });
    }
  }

  ngOnDestroy(): void {
    this.editor?.dispose();
    this.editor = null;
    this.remoteDecorations.clear();
  }

  // --- API pública para el componente padre ---

  getCurrentValue(): string {
    return this.editor?.getValue() ?? '';
  }

  focus(): void {
    this.editor?.focus();
  }

  // --- Privado ---

  private initEditor(): void {
    const monaco = window.monaco;

    this.editor = monaco.editor.create(this.containerRef.nativeElement, {
      value: this.value,
      language: this.language,
      theme: 'vs-dark',
      readOnly: this.readonly,
      automaticLayout: true,
      fontSize: 14,
      lineHeight: 22,
      fontFamily: "'Fira Code', 'Cascadia Code', 'Consolas', monospace",
      fontLigatures: true,
      minimap: { enabled: false },
      scrollBeyondLastLine: false,
      tabSize: 4,
      insertSpaces: true,
      wordWrap: 'on',
      renderLineHighlight: 'gutter',
      cursorBlinking: 'phase',
    });

    this.editor.onDidChangeModelContent(() => {
      this.valueChange.emit(this.editor!.getValue());
    });

    this.editor.onDidChangeCursorPosition((e) => {
      this.cursorChange.emit({
        lineNumber: e.position.lineNumber,
        column: e.position.column,
      });
    });
  }

  private renderRemoteCursors(cursors: CursorPosition[]): void {
    if (!this.editor) return;
    const monaco = window.monaco;

    cursors.forEach((cursor) => {
      // Eliminar decoración previa de este usuario
      this.remoteDecorations.get(cursor.uid)?.clear();

      const collection = this.editor!.createDecorationsCollection([
        {
          range: new monaco.Range(
            cursor.lineNumber,
            cursor.column,
            cursor.lineNumber,
            cursor.column + 1,
          ),
          options: {
            className: `remote-cursor`,
            // El color se aplica via una variable CSS inline generada con el uid
            hoverMessage: { value: cursor.displayName },
            zIndex: 1,
          },
        },
      ]);

      this.remoteDecorations.set(cursor.uid, collection);
    });

    // Limpiar cursores de usuarios que ya no están
    const activeUids = new Set(cursors.map((c) => c.uid));
    this.remoteDecorations.forEach((collection, uid) => {
      if (!activeUids.has(uid)) {
        collection.clear();
        this.remoteDecorations.delete(uid);
      }
    });
  }
}
