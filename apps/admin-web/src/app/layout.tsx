import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { AdminAuthProvider } from "../lib/AdminAuthContext";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Lajan'm — Back-office",
  description: "Internal operations console for Lajan'm",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={`${geistSans.variable} ${geistMono.variable}`}>
      <body>
        <AdminAuthProvider>{children}</AdminAuthProvider>
      </body>
    </html>
  );
}
