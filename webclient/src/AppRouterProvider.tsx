// src/AppRouterProvider.tsx  (or wherever you mount the router)
import { Suspense } from "react";
import { RouterProvider } from "react-router-dom";
import { router } from "./routes";

export function AppRouterProvider() {
  return (
    <Suspense fallback={<p>Loading…</p>}>
      <RouterProvider router={router} />
    </Suspense>
  );
}
