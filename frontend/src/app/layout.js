import { Geist } from "next/font/google";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

export const metadata = {
  title: {
    default: "E-commerce Storefront",
    template: "%s | E-commerce Storefront",
  },
  description: "Browse products, prices, sellers, and stock.",
};

export default function RootLayout({ children }) {
  return (
    <html
      lang="en"
      className={geistSans.variable}
      data-scroll-behavior="smooth"
    >
      <body>
        <a className="skip-link" href="#main-content">
          Skip to main content
        </a>

        {children}
      </body>
    </html>
  );
}
