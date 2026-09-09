import StoreFooter from "@/components/layout/StoreFooter/StoreFooter";
import StoreHeader from "@/components/layout/StoreHeader/StoreHeader";
import styles from "./layout.module.css";

export default function StoreLayout({ children }) {
  return (
    <div className={styles.shell}>
      <StoreHeader />

      <main id="main-content">{children}</main>

      <StoreFooter />
    </div>
  );
}
