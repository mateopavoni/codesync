import { Validators, type ValidatorFn } from '@angular/forms';

// Piso real de la política de contraseña (backend/Firebase no lo valida —
// el medidor visual en password-strength.component.ts pide más, pero eso es
// solo sugerencia; esto es lo que efectivamente bloquea el submit).
export const PASSWORD_MIN_LENGTH = 8;

export const hasDigitValidator: ValidatorFn = (control) => {
  const value = control.value as string | null;
  if (!value) return null;
  return /\d/.test(value) ? null : { missingDigit: true };
};

export const passwordValidators = [
  Validators.required,
  Validators.minLength(PASSWORD_MIN_LENGTH),
  hasDigitValidator,
];
