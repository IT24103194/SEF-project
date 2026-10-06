// Setup file for Vitest / Testing Library
import '@testing-library/jest-dom';

// Suppress known React Router v7 future warnings in test runner
const originalWarn = console.warn;
console.warn = (...args) => {
  if (
    typeof args[0] === 'string' &&
    args[0].includes('React Router Future Flag Warning')
  ) {
    return;
  }
  originalWarn(...args);
};
