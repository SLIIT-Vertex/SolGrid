# Native transaction QR workflow

The prosumer transaction screen requests a fresh verification token from `POST /api/v1/reservations/{id}/qr` for an approved booking. ZXing encodes the exact `reservationId|verificationToken` payload as a real QR, with UTF-8, medium error correction and a four-module quiet zone. The image stays black on white without clipping. The screen displays the server expiry, hides the image when it expires, and lets the prosumer generate a new code. Generating a new code invalidates older tokens in the API.

In Operator Mode, Scan Prosumer QR displays the live camera inside the existing screen with an aiming frame. It requests camera permission on entry, shows permission settings if access is denied, and continues scanning after an unrelated QR. A valid transaction payload stops scanning and opens the existing verification result. The preview pauses when the app goes into the background and releases the camera when the screen closes. QR images are not saved to storage.

The client checks only the payload format. `POST /api/v1/reservations/verify-qr` verifies approval, expiry and the supplied token against the server's stored hash. The operator then reviews the reservation and uses Finalize Energy Transfer. The completion API revalidates the token and prevents repeated completion. No authority is granted by locally decoding a QR.

Third-party libraries:

- ZXing core 3.5.3: https://github.com/zxing/zxing
- JourneyApps ZXing Android Embedded 4.3.0: https://github.com/journeyapps/zxing-android-embedded
- Android test libraries updated for the Android 16 emulator: https://developer.android.com/jetpack/androidx/releases/test

Verification includes encoding/decoding the exact payload, rejection of malformed codes, decoding the rendered Compose image at device density, and starting/stopping a real native camera preview on the emulator. A physical camera scan between prosumer and operator devices remains a useful final demonstration with an approved booking and a reachable API.
