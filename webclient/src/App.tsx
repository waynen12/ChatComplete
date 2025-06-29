import { Button } from "@/components/ui/button"
import { Toaster } from "sonner";

function App() {
  return (
    <div className="flex flex-col items-center justify-center min-h-svh">
      <Button>Click me</Button>
      <Toaster position="top-center" richColors />   {/* one line */}
    </div>
  )
}

export default App
