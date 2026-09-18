export const bookingStepOrder = [
  'prosumer',
  'grid-node',
  'slot-time',
  'review',
] as const

export type BookingStep = (typeof bookingStepOrder)[number]

interface BookingStepDetails {
  label: string
  title: string
  description: string
  nextLabel?: string
  next?: BookingStep
  previous?: BookingStep
}

export const bookingSteps: Record<BookingStep, BookingStepDetails> = {
  prosumer: {
    label: 'Prosumer',
    title: 'Who is this reservation for?',
    description:
      'Find an active prosumer and confirm their details before continuing.',
    nextLabel: 'Continue to grid node',
    next: 'grid-node',
  },
  'grid-node': {
    label: 'Grid node',
    title: 'Choose a grid node',
    description: 'Compare stations, check availability, and select a location.',
    nextLabel: 'Continue to slot & time',
    next: 'slot-time',
    previous: 'prosumer',
  },
  'slot-time': {
    label: 'Slot & time',
    title: 'Find the right slot and time',
    description:
      'View every battery slot at this station, with its capacity and time window.',
    nextLabel: 'Review reservation',
    next: 'review',
    previous: 'grid-node',
  },
  review: {
    label: 'Review',
    title: 'Everything look right?',
    description: 'Check the details below before saving your reservation.',
    previous: 'slot-time',
  },
}
