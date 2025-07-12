import {ThemeProvider} from "./context/ThemeContext";
import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";               
import { AppRouterProvider } from "./routes";
import {Toaster} from "sonner";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode> 
    <ThemeProvider>
      <Toaster richColors position="top-center" closeButton theme="system" /> 
      <AppRouterProvider /> 
    </ThemeProvider>
  </React.StrictMode>
);
