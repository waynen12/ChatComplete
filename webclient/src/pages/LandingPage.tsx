import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";

export default function LandingPage() {
  return (
    <main className="min-h-screen flex flex-col items-center justify-center bg-gradient-to-br from-primary/5  via-secondary/20 to-background">
      <section className="text-center space-y-8">
        <h1 className="text-4xl font-semibold tracking-tight">
          <span className="text-primary">AI</span> Knowledge Manager&nbsp;
        </h1>
        <p className="max-w-lg text-muted-foreground">
          Upload documents, convert them to searchable knowledge, and chat with it
          instantly.
        </p>

        <div className="flex gap-6 justify-center">
          <Button asChild size="lg">
            <Link to="/knowledge">Manage Knowledge</Link>
          </Button>
          <Button asChild variant="secondary" size="lg">
            <Link to="/chat">Chat with AI</Link>
          </Button>
        </div>
      </section>
    </main>
  );
}
