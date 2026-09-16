import logoSrc from '@/assets/logo.png'

export function Logo({ className }: { className?: string }) {
  return <img src={logoSrc} alt="SolGrid" className={className ?? 'size-11 object-contain'} />
}
