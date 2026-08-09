import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, input } from '@angular/core';

// Fondo animado en WebGL1 — puerto de diseños/componentes/grain-gradient-truchet.tsx
// (21st.dev Shader Builder, basado en Paper Shaders "Grain Gradient", Apache-2.0).
// El shader FRAG es genérico/parametrizado por uniforms; solo cambian los presets
// de color/movimiento (PRESETS abajo) y se elimina el tracking de cursor del
// original (nunca estaba activado ahí — "cursorEnabled: false" — así que era
// código muerto: listeners de pointermove/scroll/blur + easing de mouseX/mouseY).
// u_cursor queda fijo en (0,0,0,0), lo que deja inerte la rama de cursor del
// shader sin tener que tocar el GLSL.

const VERT = `attribute vec2 a_position;
void main() {
  gl_Position = vec4(a_position, 0.0, 1.0);
}`;

const FRAG = `#ifdef GL_FRAGMENT_PRECISION_HIGH
precision highp float;
#else
precision mediump float;
#endif

uniform vec3 u_colors[8];
uniform vec4 u_scene;
uniform vec4 u_shape;
uniform vec4 u_surface;
uniform vec4 u_finish;
uniform vec4 u_transform;
uniform vec4 u_space;
uniform vec4 u_cursor;

#define u_resolution u_scene.xy
#define u_time u_scene.z
#define u_colorCount u_scene.w
#define u_scale u_shape.x
#define u_intensity u_shape.y
#define u_paramA u_shape.z
#define u_warp u_shape.w
#define u_detail u_surface.x
#define u_contrast u_surface.y
#define u_brightness u_surface.z
#define u_saturation u_surface.w
#define u_hue u_finish.x
#define u_vignette u_finish.y
#define u_blur u_finish.z
#define u_grain u_finish.w
#ifdef GL_FRAGMENT_PRECISION_HIGH
#define u_seed u_transform.x
#else
#define u_seed mod(u_transform.x, 31.0)
#endif
#define u_rotate u_transform.y
#define u_drift u_transform.z
#define u_offset u_space.xy
#define u_mouse u_space.zw
#define u_cursorPresence u_cursor.x
#define u_cursorEffect u_cursor.y
#define u_cursorStrength u_cursor.z
#define u_cursorRadius u_cursor.w

float hash21(vec2 p) {
#ifndef GL_FRAGMENT_PRECISION_HIGH
  p = mod(p, 31.0);
#endif
  p = fract(p * vec2(234.34, 435.345));
  p += dot(p, p + 34.23);
  return fract(p.x * p.y);
}

float grainHash(vec2 p) {
  vec3 p3 = fract(vec3(p.xyx) * 0.1031);
  p3 += dot(p3, p3.yzx + 33.33);
  return fract((p3.x + p3.y) * p3.z);
}

float noise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  vec2 u = f * f * (3.0 - 2.0 * f);
  return mix(
    mix(hash21(i), hash21(i + vec2(1.0, 0.0)), u.x),
    mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), u.x),
    u.y);
}

float fbm(vec2 p) {
  float v = 0.0;
  float a = 0.5;
  for (int i = 0; i < 5; i++) {
    v += a * noise(p);
    p = p * 2.03 + vec2(17.0, 9.2);
    a *= 0.5;
  }
  return v;
}

vec3 palette(float x) {
  float n = max(u_colorCount - 1.0, 1.0);
  float f = clamp(x, 0.0, 1.0) * n;
  vec3 col = u_colors[0];
  for (int i = 0; i < 7; i++) {
    if (float(i) < n)
      col = mix(col, u_colors[i + 1], smoothstep(0.0, 1.0, clamp(f - float(i), 0.0, 1.0)));
  }
  return col;
}

vec3 shade(vec2 uv, vec2 p, float t) {
  vec2 q = p;
  q.x += sin(q.y * 2.1 + t * 0.2) * (0.08 + u_intensity * 0.32);
  q.y += cos(q.x * 2.7 - t * 0.17) * (0.06 + u_intensity * 0.26);
  float wave = 0.5 + 0.5 * sin(q.x * 2.8 + q.y * 1.9 + fbm(q * 2.0) * 3.0);
  float coarse = hash21(floor((uv + t * 0.002) * (90.0 + u_paramA * 260.0)) + u_seed);
  float grain = (coarse - 0.5) * u_paramA * 0.42;
  return palette(clamp(wave + grain, 0.0, 1.0));
}

void main() {
  vec2 uv = gl_FragCoord.xy / u_resolution.xy;
  vec2 screenUv = uv;
  vec2 p = (gl_FragCoord.xy - 0.5 * u_resolution.xy) / min(u_resolution.x, u_resolution.y);

  uv = p * min(u_resolution.x, u_resolution.y) / u_resolution.xy + 0.5;
  p *= u_scale;
  if (abs(u_rotate) > 0.0001) {
    float cr = cos(u_rotate), sr = sin(u_rotate);
    p = mat2(cr, -sr, sr, cr) * p;
  }
  p += u_offset;
  if (u_drift > 0.0001)
    p += u_drift * vec2(sin(u_time * 0.31), cos(u_time * 0.23));
  if (u_warp > 0.0) {
    p += u_warp * (vec2(
      fbm(p * u_detail + u_seed),
      fbm(p * u_detail + vec2(5.2, 1.3))) - 0.5);
  }

  vec3 col;
  if (u_blur > 0.0) {
    float e = u_blur;
    float pe = e * u_scale;
    vec2 uvE = vec2(e) * min(u_resolution.x, u_resolution.y) / u_resolution.xy;
    col  = shade(uv, p, u_time) * 0.36;
    col += shade(uv + vec2(uvE.x, 0.0), p + vec2(pe, 0.0), u_time) * 0.16;
    col += shade(uv - vec2(uvE.x, 0.0), p - vec2(pe, 0.0), u_time) * 0.16;
    col += shade(uv + vec2(0.0, uvE.y), p + vec2(0.0, pe), u_time) * 0.16;
    col += shade(uv - vec2(0.0, uvE.y), p - vec2(0.0, pe), u_time) * 0.16;
  } else {
    col = shade(uv, p, u_time);
  }

  if (abs(u_contrast - 1.0) > 0.0001)
    col = (col - 0.5) * u_contrast + 0.5;
  if (abs(u_saturation - 1.0) > 0.0001) {
    float luma = dot(col, vec3(0.299, 0.587, 0.114));
    col = mix(vec3(luma), col, u_saturation);
  }
  if (abs(u_brightness) > 0.0001)
    col += u_brightness;
  if (u_vignette > 0.0001) {
    float vd = length(screenUv - 0.5) * 1.41421356;
    col *= 1.0 - u_vignette * smoothstep(0.35, 1.0, vd);
  }
  if (u_grain > 0.0001)
    col += (grainHash(gl_FragCoord.xy + vec2(u_seed * 17.0, u_seed * 31.0)) - 0.5) * u_grain;
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
`;

type ColorTriple = [number, number, number];

interface ShaderPreset {
  colors: ColorTriple[];
  colorCount: number;
  scale: number;
  intensity: number;
  paramA: number;
  warp: number;
  detail: number;
  contrast: number;
  brightness: number;
  saturation: number;
  vignette: number;
  blur: number;
  grain: number;
  seed: number;
  rotate: number;
  offsetX: number;
  offsetY: number;
  drift: number;
  timeScale: number;
}

// Paleta CodeSync (apps/web/src/styles.css) en RGB 0..1 — no la del preset original.
const BG: ColorTriple = [0.043, 0.075, 0.149]; // --cs-bg #0b1326
const PRIMARY: ColorTriple = [0.678, 0.776, 1.0]; // --cs-primary #adc6ff
const TERTIARY: ColorTriple = [0.816, 0.737, 1.0]; // --cs-tertiary #d0bcff

// Paradas de acento mezcladas con BG (no el color puro): con stops muy
// alejados entre sí, palette() genera "curvas de nivel" de alto contraste
// en vez de blobs suaves — mezclar hacia BG achica esa distancia de color.
const lerp = (a: ColorTriple, b: ColorTriple, t: number): ColorTriple => [
  a[0] + (b[0] - a[0]) * t,
  a[1] + (b[1] - a[1]) * t,
  a[2] + (b[2] - a[2]) * t,
];
const SOFT_PRIMARY_HERO = lerp(BG, PRIMARY, 0.35);
const SOFT_TERTIARY_HERO = lerp(BG, TERTIARY, 0.3);
const SOFT_PRIMARY_VEIL = lerp(BG, PRIMARY, 0.2);

const PRESETS: Record<'hero' | 'veil', ShaderPreset> = {
  // Landing: wash navy lento con blooms apagados de primary/tertiary, blur
  // activo para que las transiciones sean difusas, no bandas de contorno.
  hero: {
    colors: [BG, SOFT_PRIMARY_HERO, BG, SOFT_TERTIARY_HERO, BG, BG, BG, BG],
    colorCount: 4,
    scale: 1.0,
    intensity: 0.18,
    paramA: 0.05,
    warp: 0.05,
    detail: 1.8,
    contrast: 1.0,
    brightness: 0,
    saturation: 1.0,
    vignette: 0.35,
    blur: 0.006,
    grain: 0.03,
    seed: 4,
    rotate: 0.5,
    offsetX: 0,
    offsetY: 0,
    drift: 0.03,
    timeScale: 0.3,
  },
  // Auth: todavía más calmo — no puede competir con el formulario legible encima.
  veil: {
    colors: [BG, SOFT_PRIMARY_VEIL, BG, BG, BG, BG, BG, BG],
    colorCount: 3,
    scale: 1.3,
    intensity: 0.1,
    paramA: 0.03,
    warp: 0.03,
    detail: 1.8,
    contrast: 1.0,
    brightness: -0.01,
    saturation: 1.0,
    vignette: 0.45,
    blur: 0.008,
    grain: 0.025,
    seed: 9,
    rotate: 0.25,
    offsetX: 0,
    offsetY: 0,
    drift: 0.015,
    timeScale: 0.18,
  },
};

@Component({
  selector: 'app-shader-background',
  standalone: true,
  template: `<canvas #canvas></canvas>`,
  styles: [
    `
      :host {
        position: absolute;
        inset: 0;
        display: block;
        overflow: hidden;
        pointer-events: none;
      }
      canvas {
        display: block;
        width: 100%;
        height: 100%;
      }
    `,
  ],
})
export class ShaderBackgroundComponent implements AfterViewInit, OnDestroy {
  readonly variant = input<'hero' | 'veil'>('hero');

  @ViewChild('canvas', { static: true }) private readonly canvasRef!: ElementRef<HTMLCanvasElement>;

  private gl: WebGLRenderingContext | null = null;
  private program: WebGLProgram | null = null;
  private buffer: WebGLBuffer | null = null;
  private raf = 0;
  private disposed = false;
  private resizeObserver?: ResizeObserver;
  private intersectionObserver?: IntersectionObserver;
  private visible = true;
  private inView = true;
  private cleanupListeners?: () => void;

  ngAfterViewInit(): void {
    const canvas = this.canvasRef.nativeElement;
    const gl = canvas.getContext('webgl', { antialias: false });
    if (!gl) return;
    this.gl = gl;

    const preset = PRESETS[this.variant()];
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    const compile = (type: number, src: string) => {
      const s = gl.createShader(type)!;
      gl.shaderSource(s, src);
      gl.compileShader(s);
      return s;
    };
    const program = gl.createProgram()!;
    const vertexShader = compile(gl.VERTEX_SHADER, VERT);
    const fragmentShader = compile(gl.FRAGMENT_SHADER, FRAG);
    gl.attachShader(program, vertexShader);
    gl.attachShader(program, fragmentShader);
    gl.linkProgram(program);
    gl.deleteShader(vertexShader);
    gl.deleteShader(fragmentShader);
    gl.useProgram(program);
    this.program = program;

    const buf = gl.createBuffer()!;
    this.buffer = buf;
    gl.bindBuffer(gl.ARRAY_BUFFER, buf);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
    const posLoc = gl.getAttribLocation(program, 'a_position');
    gl.enableVertexAttribArray(posLoc);
    gl.vertexAttribPointer(posLoc, 2, gl.FLOAT, false, 0, 0);

    const uni = {
      colors: gl.getUniformLocation(program, 'u_colors'),
      scene: gl.getUniformLocation(program, 'u_scene'),
      shape: gl.getUniformLocation(program, 'u_shape'),
      surface: gl.getUniformLocation(program, 'u_surface'),
      finish: gl.getUniformLocation(program, 'u_finish'),
      transform: gl.getUniformLocation(program, 'u_transform'),
      space: gl.getUniformLocation(program, 'u_space'),
      cursor: gl.getUniformLocation(program, 'u_cursor'),
    };
    gl.uniform3fv(uni.colors, new Float32Array(preset.colors.flat()));
    gl.uniform4f(uni.shape, preset.scale, preset.intensity, preset.paramA, preset.warp);
    gl.uniform4f(uni.surface, preset.detail, preset.contrast, preset.brightness, preset.saturation);
    gl.uniform4f(uni.finish, 0, preset.vignette, preset.blur, preset.grain);
    gl.uniform4f(uni.transform, preset.seed, preset.rotate, preset.drift, 0);
    gl.uniform4f(uni.space, preset.offsetX, preset.offsetY, 0, 0);
    gl.uniform4f(uni.cursor, 0, 0, 0, 0);

    let bounds = canvas.getBoundingClientRect();
    const resizeCanvas = () => {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      const rawWidth = Math.max(1, Math.round(bounds.width * dpr));
      const rawHeight = Math.max(1, Math.round(bounds.height * dpr));
      const pixelScale = Math.min(1, Math.sqrt(2_000_000 / Math.max(1, rawWidth * rawHeight)));
      const width = Math.max(1, Math.round(rawWidth * pixelScale));
      const height = Math.max(1, Math.round(rawHeight * pixelScale));
      if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width;
        canvas.height = height;
        gl.viewport(0, 0, width, height);
      }
    };

    const requestRender = () => {
      if (!this.disposed && this.visible && this.inView && this.raf === 0) {
        this.raf = requestAnimationFrame(render);
      }
    };

    const updateLayout = () => {
      bounds = canvas.getBoundingClientRect();
      resizeCanvas();
      requestRender();
    };
    window.addEventListener('resize', updateLayout);

    this.resizeObserver = new ResizeObserver(updateLayout);
    this.resizeObserver.observe(canvas);

    this.intersectionObserver = new IntersectionObserver(([entry]) => {
      this.inView = entry?.isIntersecting ?? true;
      if (this.inView) requestRender();
      else if (this.raf !== 0) {
        cancelAnimationFrame(this.raf);
        this.raf = 0;
      }
    });
    this.intersectionObserver.observe(canvas);

    const onVisibilityChange = () => {
      this.visible = document.visibilityState === 'visible';
      if (this.visible) requestRender();
      else if (this.raf !== 0) {
        cancelAnimationFrame(this.raf);
        this.raf = 0;
      }
    };
    document.addEventListener('visibilitychange', onVisibilityChange);
    this.cleanupListeners = () => {
      window.removeEventListener('resize', updateLayout);
      document.removeEventListener('visibilitychange', onVisibilityChange);
    };

    const start = performance.now();
    const render = (now: number) => {
      this.raf = 0;
      if (this.disposed || !this.visible || !this.inView) return;
      resizeCanvas();
      gl.uniform4f(uni.scene, canvas.width, canvas.height, ((now - start) / 1000) * preset.timeScale, preset.colorCount);
      gl.drawArrays(gl.TRIANGLES, 0, 3);
      // prefers-reduced-motion: un solo frame estático, nunca arranca el loop de rAF.
      if (!reducedMotion) requestRender();
    };

    updateLayout();
  }

  ngOnDestroy(): void {
    this.disposed = true;
    if (this.raf) cancelAnimationFrame(this.raf);
    this.resizeObserver?.disconnect();
    this.intersectionObserver?.disconnect();
    this.cleanupListeners?.();
    if (this.gl) {
      if (this.buffer) this.gl.deleteBuffer(this.buffer);
      if (this.program) this.gl.deleteProgram(this.program);
      this.gl.getExtension('WEBGL_lose_context')?.loseContext();
    }
  }
}
