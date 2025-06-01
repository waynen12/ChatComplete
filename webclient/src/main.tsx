import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";               
import { AppRouterProvider } from "./routes";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <AppRouterProvider />
  </React.StrictMode>
);
