import {
  createBrowserRouter,
  RouterProvider,  
} from "react-router-dom";
import type { RouteObject } from "react-router-dom";
import AppLayout from "./layouts/AppLayout";
import LandingPage from "./pages/LandingPage";
import KnowledgeListPage from "./pages/KnowledgeListPage";
import KnowledgeFormPage from "./pages/KnowledgeFormPage";
import ChatPage from "./pages/ChatPage";
import NotFoundPage from "./pages/NotFoundPage";
import { lazy, Suspense } from "react";

const routes: RouteObject[] = [
  {
    path: "/",
    element: <AppLayout />,
    errorElement: <NotFoundPage />, // fallback for unknown routes
    children: [
      { index: true, element: <LandingPage /> },
      {
        path: "knowledge",
        children: [
          { index: true, element: <KnowledgeListPage /> },
          { path: "new", element: <KnowledgeFormPage /> },
          { path: ":id/edit", element: <KnowledgeFormPage /> },
        ],
      },
      {
        path: "chat",
        children: [
          { index: true, element: <ChatPage /> },
          { path: ":id", element: <ChatPage /> },
        ],
      },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
];

export const router = createBrowserRouter(routes);

// routes.tsx (or wherever you mount the router)
export function AppRouterProvider() {
  return (
    <Suspense fallback={<p className="p-4">Loading…</p>}>
      <RouterProvider router={router} />
    </Suspense>
  );
}

