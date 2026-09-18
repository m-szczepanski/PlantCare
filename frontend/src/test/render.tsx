import { type ReactElement } from "react";
import { render, type RenderOptions } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";

export function renderWithProviders(
  ui: ReactElement,
  options?: RenderOptions & { route?: string; path?: string },
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  const { route = "/", path, ...renderOptions } = options ?? {};

  const tree = path ? (
    <MemoryRouter initialEntries={[route]}>
      <Routes>
        <Route path={path} element={ui} />
      </Routes>
    </MemoryRouter>
  ) : (
    <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
  );

  return render(<QueryClientProvider client={queryClient}>{tree}</QueryClientProvider>, renderOptions);
}
