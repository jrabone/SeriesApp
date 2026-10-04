import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "SeriesApp",
  description: "SeriesApp web client",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en">
      <body className="min-h-dvh bg-background text-foreground antialiased">
        {children}
      </body>
    </html>
  );
}
