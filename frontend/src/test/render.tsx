import { type ReactElement } from "react";
import { render, type RenderOptions } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";

export function renderWithProviders(ui: ReactElement, options?: RenderOptions & { route?: string }) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  const { route = "/", ...renderOptions } = options ?? {};

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
    </QueryClientProvider>,
    renderOptions,
  );
}
