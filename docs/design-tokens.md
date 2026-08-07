# CodeSync — Design Tokens

Fuente: Stitch AI export (`codesync_alpha/DESIGN.md`, `codesync_landing_page`, `codesync_dashboard`, `codesync_collaborative_editor`).
Implementado en `apps/web/src/styles.css` como CSS custom properties con prefijo `--cs-*`.

---

## Color — Superficie (tonal elevation)

El sistema usa capas de superficie en vez de sombras. La profundidad se comunica con el tono.

| Token | Valor | Uso |
|---|---|---|
| `--cs-bg` / `--cs-surface` | `#0b1326` | Fondo base de toda la app |
| `--cs-surface-lowest` | `#060e20` | Fondo más profundo (hero dark) |
| `--cs-surface-low` | `#131b2e` | Paneles laterales, cards secundarias |
| `--cs-surface-container` | `#171f33` | Cards, inputs, paneles de chat/coach |
| `--cs-surface-high` | `#222a3d` | Filas de tabla en hover |
| `--cs-surface-highest` / `--cs-surface-variant` | `#2d3449` | Tooltips, modales |
| `--cs-surface-bright` | `#31394d` | Hover states sobre surface-high |

## Color — Texto

| Token | Valor | Uso |
|---|---|---|
| `--cs-on-bg` / `--cs-on-surface` | `#dae2fd` | Texto principal sobre fondos oscuros |
| `--cs-on-surface-var` | `#c2c6d6` | Texto secundario, labels, timestamps |

## Color — Bordes

| Token | Valor | Uso |
|---|---|---|
| `--cs-outline` | `#8c909f` | Borde visible (hover, separadores) |
| `--cs-outline-var` | `#424754` | Borde sutil (cards, inputs en reposo) |

## Color — Primario (azul)

Acciones principales, links activos, focus ring, sidebar active state.

| Token | Valor |
|---|---|
| `--cs-primary` | `#adc6ff` |
| `--cs-on-primary` | `#002e6a` |
| `--cs-primary-container` | `#4d8eff` |

## Color — Secundario (verde)

Exito, completado, tests pasados, botón "Ejecutar", sala activa.

| Token | Valor |
|---|---|
| `--cs-secondary` | `#4edea3` |
| `--cs-on-secondary` | `#003824` |
| `--cs-secondary-cont` | `#00a572` |
| `--cs-on-secondary-cont` | `#00311f` |

## Color — Terciario (purpura)

Exclusivo para el IA Coach. No usar en otros contextos.

| Token | Valor |
|---|---|
| `--cs-tertiary` | `#d0bcff` |
| `--cs-on-tertiary` | `#3c0091` |
| `--cs-tertiary-cont` | `#a078ff` |
| `--cs-on-tertiary-cont` | `#340080` |

## Color — Error / semántico

| Token | Valor | Uso |
|---|---|---|
| `--cs-error` | `#ffb4ab` | Texto de error, icono de error, tests fallados |
| `--cs-on-error` | `#690005` | Texto sobre fondo de error |
| `--cs-error-cont` | `#93000a` | Fondo de error container |
| `--cs-on-error-cont` | `#ffdad6` | Texto sobre error-container |

## Color — Warning (sin token, unico uso)

El badge "Medio" usa `#f59e0b` con `rgba(245, 158, 11, 0.15)` de fondo. Sin token dedicado porque solo aparece en badges de dificultad.

---

## Tipografia

Dos familias. Sin mezclar en un mismo contexto.

| Token | Valor | Uso |
|---|---|---|
| `--cs-font-ui` | `'Inter', system-ui, sans-serif` | Todo texto de interfaz |
| `--cs-font-code` | `'JetBrains Mono', monospace` | Codigo, entradas/salidas de tests, tags de lenguaje |

### Escala

| Token | px | line-height | letter-spacing | Uso |
|---|---|---|---|---|
| `--cs-text-display` | 32 | 40px | -0.02em | Headlines de landing (hero) |
| `--cs-text-headline` | 20 | 28px | — | Titulos de seccion |
| `--cs-text-body` | 16 | 24px | — | Parrafos generales |
| `--cs-text-body-sm` | 14 | 20px | — | Body de cards, listas |
| `--cs-text-label` | 12 | 16px | 0.05em | Labels de seccion (uppercase), timestamps |
| `--cs-text-code` | 14 | 22px | — | Codigo en panel de editor |
| `--cs-text-code-sm` | 12 | 18px | — | Valores en tests, inputs/outputs |

Pesos usados: 400 (body), 500 (emphasis), 600 (labels/caps), 700 (headings).

---

## Espaciado

Base 4 px. Usar estos tokens; no valores arbitrarios.

| Token | Valor |
|---|---|
| `--cs-sp-xs` | 4px |
| `--cs-sp-sm` | 8px |
| `--cs-sp-md` | 16px |
| `--cs-sp-lg` | 24px |
| `--cs-sp-xl` | 32px |

---

## Border Radius

| Token | Valor | Uso |
|---|---|---|
| `--cs-radius-sm` | 2px | Indicadores tiny (pulsos, dots) |
| `--cs-radius-md` | 4px | Focus ring, badges de tag |
| `--cs-radius-lg` | 8px | Cards, inputs, paneles, modales |
| `--cs-radius-xl` | 12px | Buttons, badges de dificultad, pills |

---

## Layout

| Token | Valor | Uso |
|---|---|---|
| `--cs-sidebar` | 260px | Ancho fijo del sidebar de navegacion |
| `--cs-topbar` | 40px | Alto del topbar mobile |

El layout usa un hibrido fijo-fluido: sidebar `position: fixed` + `main { margin-left: 260px }`. En mobile (< 768px): sidebar oculto, topbar visible, margin-left eliminado.

---

## Iconos

Material Symbols Outlined (variable font, Google Fonts). Configuracion base:

```css
.material-symbols-outlined {
  font-variation-settings: 'FILL' 0, 'wght' 300, 'GRAD' 0, 'opsz' 24;
}
```

Regla: `aria-hidden="true"` en todo icono decorativo. Si el icono es el unico indicador de estado, agregar texto `.sr-only` o `aria-label` al contenedor.

---

## Elevacion

No hay drop shadows. La elevacion se expresa con el salto de superficie:

- Fondo base: `--cs-bg`
- Panel/card: `--cs-surface-low` o `--cs-surface-container`
- Modal/dropdown: `--cs-surface-highest`

El acento izquierdo (`border-left: 3px solid <color>`) en lugar de shadow para destacar cards especiales (dashboard feedback, IA Coach).

---

## Aliases de compatibilidad

Componentes legacy con estilos inline usan `--color-*`. Estos apuntan a los tokens nuevos para no romper nada:

| Alias | Apunta a |
|---|---|
| `--color-bg` | `--cs-bg` |
| `--color-surface` | `--cs-surface-low` |
| `--color-surface-2` | `--cs-surface-container` |
| `--color-border` | `--cs-outline-var` |
| `--color-text` | `--cs-on-surface` |
| `--color-text-secondary` | `--cs-on-surface-var` |
| `--color-accent` | `--cs-primary` |

No crear mas aliases. Componentes nuevos usan `--cs-*` directo.

---

## Decisiones de diseno

- **Tema unico (dark):** no hay toggle. Diseno de IDE — el dark es la identidad del producto.
- **Color reservado por rol:** verde = exito/codigo, purpura = IA, azul = navegacion/acciones. No mezclar.
- **Motion:** `prefers-reduced-motion` global elimina todas las animaciones. Transiciones maximas: 150-200ms, `ease` o `linear`. Sin bounce ni spring en UI utilitaria.
- **Fuentes externas:** Inter + JetBrains Mono se cargan desde Google Fonts en `index.html` con `preconnect`. Si el proyecto se despliega offline, reemplazar por fuentes locales.
