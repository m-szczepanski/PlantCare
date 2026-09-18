import { useEffect, useState } from "react";
import { api } from "../api/client";

type ApiStatus = "checking" | "online" | "offline";

export default function Home() {
  const [status, setStatus] = useState<ApiStatus>("checking");

  useEffect(() => {
    api
      .getHealth()
      .then(() => setStatus("online"))
      .catch(() => setStatus("offline"));
  }, []);

  return (
    <main style={{ fontFamily: "system-ui, sans-serif", padding: "2rem" }}>
      <h1>PlantCare</h1>
      <p>API status: {status}</p>
    </main>
  );
}
