import React from 'react';
import { render, type RenderOptions } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '@/contexts/AuthContext';
import { Toaster } from 'react-hot-toast';

interface WrapperProps {
  children: React.ReactNode;
  initialEntries?: string[];
}

function createWrapper(options?: { initialEntries?: string[] }) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={options?.initialEntries ?? ['/']}>
          <AuthProvider>
            {children}
            <Toaster />
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );
  };
}

function customRender(
  ui: React.ReactElement,
  options?: RenderOptions & WrapperProps,
) {
  const { initialEntries, ...rest } = options ?? {};
  return render(ui, {
    wrapper: createWrapper({ initialEntries }),
    ...rest,
  });
}

export { customRender as render };
export * from '@testing-library/react';
