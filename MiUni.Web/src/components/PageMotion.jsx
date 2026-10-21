import {useEffect} from 'react';
import {useLocation} from 'react-router-dom';

export default function PageMotion({children}) {
  const {pathname} = useLocation();
  useEffect(() => {
    const elements = [...document.querySelectorAll('[data-reveal]')];
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches || !('IntersectionObserver' in window)) return;
    const observer = new IntersectionObserver(entries => entries.forEach(entry => {
      if (entry.isIntersecting) { entry.target.dataset.reveal = 'visible'; observer.unobserve(entry.target); }
    }), {threshold: .06});
    elements.forEach(element => { element.dataset.reveal = 'pending'; observer.observe(element); });
    return () => { observer.disconnect(); elements.forEach(element => { element.dataset.reveal = 'visible'; }); };
  }, [pathname]);
  return <div key={pathname} className="route-frame">{children}</div>;
}
