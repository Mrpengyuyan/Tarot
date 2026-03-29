// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom';

if (typeof window !== 'undefined' && typeof window.matchMedia !== 'function') {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      media: query,
      matches: false,
      onchange: null,
      addListener: () => {
        // Deprecated API for older component libraries.
      },
      removeListener: () => {
        // Deprecated API for older component libraries.
      },
      addEventListener: () => {
        // Modern API used by current code.
      },
      removeEventListener: () => {
        // Modern API used by current code.
      },
      dispatchEvent: () => false,
    }),
  });
}
