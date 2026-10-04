export default function Home() {
  return (
    <main className="mx-auto flex min-h-dvh max-w-2xl flex-col justify-center gap-3 px-6">
      <h1 className="text-3xl font-semibold tracking-tight">SeriesApp</h1>
      <p className="text-sm text-foreground/60">
        Web client scaffold. No features yet &mdash; edit{" "}
        <code className="rounded bg-foreground/10 px-1.5 py-0.5 font-mono text-xs">
          src/app/page.tsx
        </code>{" "}
        to begin.
      </p>
    </main>
  );
}
