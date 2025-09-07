import { NavLink, Outlet } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { ThemeToggle } from "@/components/ThemeToggle";

export default function AppLayout() {
  return (
    <div className="flex flex-col min-h-screen">
      <header className="border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <nav className="container h-14 flex items-center justify-between">
          <NavLink to="/" className="font-semibold">
            ChatComplete
          </NavLink>
          <div className="flex gap-2">
            <Button asChild variant="ghost" size="sm">
              <NavLink to="/knowledge">Knowledge</NavLink>
            </Button>
            <Button asChild variant="ghost" size="sm">
              <NavLink to="/chat">Chat</NavLink>
            </Button>
            <Button asChild variant="ghost" size="sm">
              <NavLink to="/analytics">Analytics</NavLink>
            </Button>
            <ThemeToggle />
          </div>
        </nav>
      </header>

      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  );
}
