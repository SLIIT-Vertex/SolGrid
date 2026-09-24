/*
 * Project: SolGrid
 * File: seed-sri-lanka.js
 * Description: Seeds a large, rerunnable Sri Lankan dataset into MongoDB.
 * Usage: mongosh "<connection-string>" --file scripts/seed-sri-lanka.js
 * Optional environment: MONGODB_DATABASE, SEED_PROSUMERS, SEED_HISTORY_PER_SLOT.
 */

const databaseName = process.env.MONGODB_DATABASE || "SolGrid";
const prosumerCount = readInteger("SEED_PROSUMERS", 300, 40, 5000);
const historyPerSlot = readInteger("SEED_HISTORY_PER_SLOT", 3, 1, 20);
const database = db.getSiblingDB(databaseName);

const collections = {
    users: database.getCollection("Users"),
    prosumers: database.getCollection("Prosumers"),
    stations: database.getCollection("SolarStationInfo"),
    slots: database.getCollection("EnergyBookingSlots"),
    reservations: database.getCollection("EnergyReservations")
};

const UserRole = { Backoffice: 1, GridOperator: 2 };
const AccountStatus = { Active: 1, Inactive: 2 };
const ProsumerStatus = { Pending: 1, Active: 2, DeactivationRequested: 3, Deactivated: 4 };
const StationStatus = { Active: 1, Inactive: 2 };
const SlotStatus = { Available: 1, Reserved: 2, Occupied: 3, OutOfService: 4 };
const ReservationStatus = { Pending: 1, Approved: 2, Rejected: 3, Cancelled: 4, Completed: 5 };

// Local seed credential shared by generated users and prosumers: SolGrid@2026!
const seededPasswordHash = "$2y$11$kxLkGGiaKA4J8ShuPCXd0OPaPM7OnS1wQv6m7w55OUUkp.BjvvP2a";
const generatedIdPrefixes = {
    user: "6f12ab01",
    station: "57a71001",
    slot: "51070001",
    activeReservation: "ae5e0001",
    historicalReservation: "a1570001"
};
const now = new Date();

const places = [
    { code: "KOT", city: "Kotikawatta", district: "Colombo", province: "Western", lat: 6.9260, lng: 79.9180 },
    { code: "BAL", city: "Balangoda", district: "Ratnapura", province: "Sabaragamuwa", lat: 6.6461, lng: 80.7007 },
    { code: "KTW", city: "Kottawa", district: "Colombo", province: "Western", lat: 6.8406, lng: 79.9655 },
    { code: "MLB", city: "Malabe", district: "Colombo", province: "Western", lat: 6.9041, lng: 79.9547 },
    { code: "RJG", city: "Rajagiriya", district: "Colombo", province: "Western", lat: 6.9097, lng: 79.8943 },
    { code: "SJK", city: "Sri Jayawardenepura Kotte", district: "Colombo", province: "Western", lat: 6.8941, lng: 79.9025 },
    { code: "NUG", city: "Nugegoda", district: "Colombo", province: "Western", lat: 6.8649, lng: 79.8997 },
    { code: "EMB", city: "Embilipitiya", district: "Ratnapura", province: "Sabaragamuwa", lat: 6.3439, lng: 80.8489 },
    { code: "PLY", city: "Piliyandala", district: "Colombo", province: "Western", lat: 6.8018, lng: 79.9227 },
    { code: "KAN", city: "Kandy", district: "Kandy", province: "Central", lat: 7.2906, lng: 80.6337 },
    { code: "GAL", city: "Galle", district: "Galle", province: "Southern", lat: 6.0329, lng: 80.2168 },
    { code: "MAT", city: "Matara", district: "Matara", province: "Southern", lat: 5.9549, lng: 80.5550 },
    { code: "JAF", city: "Jaffna", district: "Jaffna", province: "Northern", lat: 9.6615, lng: 80.0255 },
    { code: "ANU", city: "Anuradhapura", district: "Anuradhapura", province: "North Central", lat: 8.3114, lng: 80.4037 },
    { code: "TRI", city: "Trincomalee", district: "Trincomalee", province: "Eastern", lat: 8.5874, lng: 81.2152 },
    { code: "AMP", city: "Ampara", district: "Ampara", province: "Eastern", lat: 7.2917, lng: 81.6724 },
    { code: "BAD", city: "Badulla", district: "Badulla", province: "Uva", lat: 6.9934, lng: 81.0550 },
    { code: "HAM", city: "Hambantota", district: "Hambantota", province: "Southern", lat: 6.1241, lng: 81.1185 },
    { code: "CMB", city: "Colombo", district: "Colombo", province: "Western", lat: 6.9271, lng: 79.8612 },
    { code: "NEG", city: "Negombo", district: "Gampaha", province: "Western", lat: 7.2083, lng: 79.8358 },
    { code: "GAM", city: "Gampaha", district: "Gampaha", province: "Western", lat: 7.0873, lng: 80.0144 },
    { code: "KUR", city: "Kurunegala", district: "Kurunegala", province: "North Western", lat: 7.4863, lng: 80.3647 },
    { code: "RAT", city: "Ratnapura", district: "Ratnapura", province: "Sabaragamuwa", lat: 6.7056, lng: 80.3847 },
    { code: "NUE", city: "Nuwara Eliya", district: "Nuwara Eliya", province: "Central", lat: 6.9497, lng: 80.7891 },
    { code: "BAT", city: "Batticaloa", district: "Batticaloa", province: "Eastern", lat: 7.7170, lng: 81.7000 },
    { code: "POL", city: "Polonnaruwa", district: "Polonnaruwa", province: "North Central", lat: 7.9403, lng: 81.0188 },
    { code: "DAM", city: "Dambulla", district: "Matale", province: "Central", lat: 7.8742, lng: 80.6511 },
    { code: "MTL", city: "Matale", district: "Matale", province: "Central", lat: 7.4675, lng: 80.6234 },
    { code: "KAL", city: "Kalutara", district: "Kalutara", province: "Western", lat: 6.5854, lng: 79.9607 },
    { code: "PAN", city: "Panadura", district: "Kalutara", province: "Western", lat: 6.7132, lng: 79.9026 },
    { code: "HOR", city: "Horana", district: "Kalutara", province: "Western", lat: 6.7159, lng: 80.0626 },
    { code: "AVI", city: "Avissawella", district: "Colombo", province: "Western", lat: 6.9553, lng: 80.2113 },
    { code: "CHI", city: "Chilaw", district: "Puttalam", province: "North Western", lat: 7.5758, lng: 79.7953 },
    { code: "PUT", city: "Puttalam", district: "Puttalam", province: "North Western", lat: 8.0408, lng: 79.8394 },
    { code: "VAV", city: "Vavuniya", district: "Vavuniya", province: "Northern", lat: 8.7514, lng: 80.4971 },
    { code: "MAN", city: "Mannar", district: "Mannar", province: "Northern", lat: 8.9810, lng: 79.9044 },
    { code: "KIL", city: "Kilinochchi", district: "Kilinochchi", province: "Northern", lat: 9.3803, lng: 80.3770 },
    { code: "MUL", city: "Mullaitivu", district: "Mullaitivu", province: "Northern", lat: 9.2671, lng: 80.8142 },
    { code: "MON", city: "Monaragala", district: "Monaragala", province: "Uva", lat: 6.8728, lng: 81.3507 },
    { code: "KEG", city: "Kegalle", district: "Kegalle", province: "Sabaragamuwa", lat: 7.2513, lng: 80.3464 }
];

const firstNames = [
    "Amaya", "Kasun", "Nadeesha", "Tharindu", "Ishara", "Dinithi", "Sahan", "Kavindi",
    "Nimal", "Sanduni", "Aravind", "Karthika", "Tharshan", "Yalini", "Fathima", "Rizwan",
    "Hareesha", "Kavishi", "Akeel", "Nusra", "Chamara", "Sachini", "Lakshan", "Imesha",
    "Dilshan", "Bawanthi"
];
const lastNames = [
    "Perera", "Fernando", "Silva", "Jayasinghe", "Bandara", "Dissanayake", "Wijesinghe", "Gunawardena",
    "Ratnayake", "Ekanayake", "Sivakumar", "Tharmalingam", "Nadarajah", "Jeyarajah", "Mohamed", "Hameed",
    "Gunasekara", "Godage", "Faizal", "Rauff", "Senanayake", "Karunaratne", "Madushanka", "Abeysekara",
    "Ranasinghe", "Herath"
];
const roadNames = [
    "Main Street", "Station Road", "Lake Road", "Temple Road", "New Town Road", "Hospital Road",
    "Market Road", "Park Road", "Kandy Road", "Galle Road", "Colombo Road", "Circular Road"
];

// Read and validate a bounded integer environment option.
function readInteger(name, fallback, minimum, maximum) {
    const raw = process.env[name];
    if (raw === undefined || raw === "") {
        return fallback;
    }

    const value = Number(raw);
    if (!Number.isInteger(value) || value < minimum || value > maximum) {
        throw new Error(`${name} must be an integer between ${minimum} and ${maximum}.`);
    }

    return value;
}

// Return a new UTC timestamp offset from the seed run time.
function addMinutes(date, minutes) {
    return new Date(date.getTime() + minutes * 60 * 1000);
}

// Format a number with stable leading zeroes for readable deterministic IDs.
function pad(value, width) {
    return String(value).padStart(width, "0");
}

// Build deterministic identifiers with the same 32-character shape as API-generated IDs.
function createId(prefix, value) {
    return `${prefix}${value.toString(16).padStart(24, "0")}`;
}

// Build a plausible synthetic Sri Lankan NIC without using a real person's identity.
function createNic(index) {
    const year = 1970 + (index % 35);
    const dayOfYear = 1 + ((index * 37) % 365) + (index % 2 === 0 ? 0 : 500);
    const serial = 1000 + (index % 9000);
    const checkDigit = (index * 7) % 10;
    return `${year}${pad(dayOfYear, 3)}${pad(serial, 4)}${checkDigit}`;
}

// Build a stable 64-character hexadecimal value for seeded QR hash fields.
function createTokenHash(seed) {
    const chunk = (seed * 2654435761 >>> 0).toString(16).padStart(8, "0");
    return chunk.repeat(8);
}

// Return a normal seven-day operating schedule with shorter weekend hours.
function createSchedule() {
    return [
        { Day: 0, OpensAt: "08:00:00", ClosesAt: "17:00:00" },
        { Day: 1, OpensAt: "06:00:00", ClosesAt: "20:00:00" },
        { Day: 2, OpensAt: "06:00:00", ClosesAt: "20:00:00" },
        { Day: 3, OpensAt: "06:00:00", ClosesAt: "20:00:00" },
        { Day: 4, OpensAt: "06:00:00", ClosesAt: "20:00:00" },
        { Day: 5, OpensAt: "06:00:00", ClosesAt: "20:00:00" },
        { Day: 6, OpensAt: "07:00:00", ClosesAt: "18:00:00" }
    ];
}

// Create realistic web users while leaving the application's configured seed users untouched.
function createUsers() {
    const users = [];
    for (let index = 1; index <= 18; index += 1) {
        const role = index <= 8 ? UserRole.Backoffice : UserRole.GridOperator;
        const personIndex = index - 1;
        const createdAt = addMinutes(now, -(90 + index) * 24 * 60);
        users.push({
            _id: createId(generatedIdPrefixes.user, index),
            FirstName: firstNames[personIndex],
            LastName: lastNames[personIndex],
            Email: `${firstNames[personIndex]}.${lastNames[personIndex]}@solgrid.lk`.toLowerCase(),
            PasswordHash: seededPasswordHash,
            Role: role,
            AccountStatus: index % 7 === 0 ? AccountStatus.Inactive : AccountStatus.Active,
            CreatedAtUtc: createdAt,
            UpdatedAtUtc: index % 7 === 0 ? addMinutes(createdAt, 45 * 24 * 60) : createdAt
        });
    }

    return users;
}

// Create prosumers across every supported account lifecycle state.
function createProsumers() {
    const prosumers = [];
    for (let index = 1; index <= prosumerCount; index += 1) {
        const createdAt = addMinutes(now, -(30 + (index % 700)) * 24 * 60);
        const statusSelector = index % 20;
        const status = statusSelector < 2
            ? ProsumerStatus.Pending
            : statusSelector === 2
                ? ProsumerStatus.DeactivationRequested
                : statusSelector === 3
                    ? ProsumerStatus.Deactivated
                    : ProsumerStatus.Active;
        const firstName = firstNames[(index - 1) % firstNames.length];
        const lastName = lastNames[(index * 7) % lastNames.length];
        const phonePrefix = ["070", "071", "072", "074", "075", "076", "077", "078"][index % 8];

        prosumers.push({
            _id: createNic(index),
            FirstName: firstName,
            LastName: lastName,
            Email: `${firstName}.${lastName}.${pad(index, 4)}@solgridmail.lk`.toLowerCase(),
            PhoneNumber: index % 17 === 0 ? null : `${phonePrefix}${pad(1000000 + index, 7)}`,
            PasswordHash: seededPasswordHash,
            AccountStatus: status,
            CreatedAtUtc: createdAt,
            UpdatedAtUtc: status === ProsumerStatus.Active ? createdAt : addMinutes(createdAt, 7 * 24 * 60)
        });
    }

    return prosumers;
}

// Convert a slot to the nested representation held by SolarStationInfo.
function copySlot(slot) {
    return {
        _id: slot._id,
        StationId: slot.StationId,
        SlotNumber: slot.SlotNumber,
        BatteryCapacityKwh: slot.BatteryCapacityKwh,
        StartTimeUtc: slot.StartTimeUtc,
        EndTimeUtc: slot.EndTimeUtc,
        Status: slot.Status,
        CreatedAtUtc: slot.CreatedAtUtc,
        UpdatedAtUtc: slot.UpdatedAtUtc
    };
}

// Create stations and their authoritative standalone slot records.
function createStationsAndSlots() {
    const stations = [];
    const slots = [];
    const slotsByStation = new Map();
    const startOffsets = [-0.25, 4, 9, 14, 26, 38, 50, 74, 98, 122, 146, 190];

    places.forEach((place, placeIndex) => {
        const stationNumber = placeIndex + 1;
        const stationId = createId(generatedIdPrefixes.station, stationNumber);
        const createdAt = addMinutes(now, -(180 + placeIndex * 8) * 24 * 60);
        const status = stationNumber % 6 === 0 ? StationStatus.Inactive : StationStatus.Active;
        const stationSlots = [];

        startOffsets.forEach((hours, slotIndex) => {
            const slotNumber = slotIndex + 1;
            const startTime = addMinutes(now, hours * 60);
            const slot = {
                _id: createId(generatedIdPrefixes.slot, placeIndex * 100 + slotNumber),
                StationId: stationId,
                SlotNumber: slotNumber,
                BatteryCapacityKwh: Decimal128(`${40 + ((placeIndex + slotIndex) % 9) * 10}.0`),
                StartTimeUtc: startTime,
                EndTimeUtc: addMinutes(startTime, slotIndex === 0 ? 60 : 90),
                Status: slotNumber === 7 || (slotNumber === 12 && stationNumber % 4 === 0)
                    ? SlotStatus.OutOfService
                    : SlotStatus.Available,
                CreatedAtUtc: createdAt,
                UpdatedAtUtc: createdAt
            };
            slots.push(slot);
            stationSlots.push(slot);
        });

        slotsByStation.set(stationId, stationSlots);
        stations.push({
            _id: stationId,
            Code: `SG-${place.code}-${pad(stationNumber, 2)}`,
            Name: `${place.city} Solar Exchange`,
            AddressLine: `No. ${12 + placeIndex * 3}, ${roadNames[placeIndex % roadNames.length]}, ${place.city}, ${place.district} District, ${place.province} Province`,
            Latitude: place.lat,
            Longitude: place.lng,
            GeoLocation: { type: "Point", coordinates: [place.lng, place.lat] },
            CapacityKw: Decimal128(`${150 + (placeIndex % 12) * 50}.0`),
            Status: status,
            TotalSlotCount: stationSlots.length,
            AvailableSlotCount: 0,
            Slots: [],
            Schedule: createSchedule(),
            CreatedAtUtc: createdAt,
            UpdatedAtUtc: status === StationStatus.Inactive ? addMinutes(now, -14 * 24 * 60) : createdAt
        });
    });

    return { stations, slots, slotsByStation };
}

// Create one valid active reservation and align its slot state.
function createActiveReservation(sequence, station, slot, prosumer, kind, backofficeId) {
    const createdAt = addMinutes(now, -(180 + sequence % 1440));
    const isPending = kind === "pending";
    const isOccupied = kind === "verified";
    const approvedAt = isPending ? null : addMinutes(createdAt, 30);
    const issuedAt = kind === "liveQr"
        ? addMinutes(now, -5)
        : kind === "expiredQr"
            ? addMinutes(now, -70)
            : kind === "verified"
                ? addMinutes(now, -15)
                : null;
    const expiresAt = issuedAt === null ? null : addMinutes(issuedAt, 30);
    const verifiedAt = kind === "verified" ? addMinutes(now, -5) : null;
    const updatedAt = verifiedAt || issuedAt || approvedAt || createdAt;

    slot.Status = isOccupied ? SlotStatus.Occupied : SlotStatus.Reserved;
    slot.UpdatedAtUtc = updatedAt;

    return {
        _id: createId(generatedIdPrefixes.activeReservation, sequence),
        ReferenceCode: `SG-A${pad(sequence, 7)}`,
        ProsumerId: prosumer._id,
        StationId: station._id,
        BookingSlotId: slot._id,
        ScheduledAtUtc: slot.StartTimeUtc,
        Status: isPending ? ReservationStatus.Pending : ReservationStatus.Approved,
        IsActiveForBookingSlot: true,
        Version: isPending ? 0 : (isOccupied ? 3 : issuedAt === null ? 1 : 2),
        CreatedAtUtc: createdAt,
        UpdatedAtUtc: updatedAt,
        ApprovedAtUtc: approvedAt,
        ApprovedBy: isPending ? null : backofficeId,
        RejectedAtUtc: null,
        RejectedBy: null,
        RejectionReason: null,
        CancelledAtUtc: null,
        CompletedAtUtc: null,
        CompletedBy: null,
        QrVerificationTokenHash: issuedAt === null ? null : createTokenHash(sequence),
        QrVerificationTokenIssuedAtUtc: issuedAt,
        QrVerificationTokenExpiresAtUtc: expiresAt,
        QrVerifiedAtUtc: verifiedAt
    };
}

// Create terminal reservation history with realistic lifecycle metadata.
function createHistoricalReservation(sequence, station, slot, prosumer, status, backofficeId, operatorId) {
    const daysAgo = 2 + (sequence % 240);
    const scheduledAt = addMinutes(now, -daysAgo * 24 * 60);
    const createdAt = addMinutes(scheduledAt, -(2 + sequence % 6) * 24 * 60);
    const approvedAt = status === ReservationStatus.Completed || (status === ReservationStatus.Cancelled && sequence % 2 === 0)
        ? addMinutes(createdAt, 120)
        : null;
    const rejectedAt = status === ReservationStatus.Rejected ? addMinutes(createdAt, 180) : null;
    const cancelledAt = status === ReservationStatus.Cancelled ? addMinutes(scheduledAt, -(13 + sequence % 30) * 60) : null;
    const completedAt = status === ReservationStatus.Completed ? addMinutes(scheduledAt, 75) : null;
    const issuedAt = status === ReservationStatus.Completed ? addMinutes(scheduledAt, -20) : null;
    const verifiedAt = status === ReservationStatus.Completed ? addMinutes(scheduledAt, 5) : null;
    const updatedAt = completedAt || cancelledAt || rejectedAt || createdAt;

    return {
        _id: createId(generatedIdPrefixes.historicalReservation, sequence),
        ReferenceCode: `SG-H${pad(sequence, 7)}`,
        ProsumerId: prosumer._id,
        StationId: station._id,
        BookingSlotId: slot._id,
        ScheduledAtUtc: scheduledAt,
        Status: status,
        IsActiveForBookingSlot: false,
        Version: status === ReservationStatus.Completed ? 4 : approvedAt === null ? 1 : 2,
        CreatedAtUtc: createdAt,
        UpdatedAtUtc: updatedAt,
        ApprovedAtUtc: approvedAt,
        ApprovedBy: approvedAt === null ? null : backofficeId,
        RejectedAtUtc: rejectedAt,
        RejectedBy: rejectedAt === null ? null : backofficeId,
        RejectionReason: rejectedAt === null
            ? null
            : ["Requested capacity is unavailable.", "Station maintenance overlaps the requested time.", "Booking details require correction."][sequence % 3],
        CancelledAtUtc: cancelledAt,
        CompletedAtUtc: completedAt,
        CompletedBy: completedAt === null ? null : operatorId,
        QrVerificationTokenHash: issuedAt === null ? null : createTokenHash(sequence + 100000),
        QrVerificationTokenIssuedAtUtc: issuedAt,
        QrVerificationTokenExpiresAtUtc: issuedAt === null ? null : addMinutes(issuedAt, 30),
        QrVerifiedAtUtc: verifiedAt
    };
}

// Generate all reservation states without double-booking an active slot.
function createReservations(stations, slotsByStation, prosumers, backofficeId, operatorId) {
    const reservations = [];
    const activeProsumers = prosumers.filter(item => item.AccountStatus === ProsumerStatus.Active);
    let activeSequence = 1;
    let historySequence = 1;

    stations.forEach((station, stationIndex) => {
        const stationSlots = slotsByStation.get(station._id);
        if (station.Status === StationStatus.Active) {
            const scenarios = [
                { slotIndex: 1, kind: "pending" },
                { slotIndex: 2, kind: "approved" },
                ...(stationIndex % 2 === 0 ? [{ slotIndex: 3, kind: "pending" }] : []),
                ...(stationIndex % 3 === 0 ? [{ slotIndex: 4, kind: "liveQr" }] : []),
                ...(stationIndex % 4 === 0 ? [{ slotIndex: 7, kind: "pending" }] : []),
                ...(stationIndex % 5 === 0 ? [{ slotIndex: 8, kind: "expiredQr" }] : []),
                ...(stationIndex % 6 === 0 ? [{ slotIndex: 0, kind: "verified" }] : []),
                ...(stationIndex === 0 ? [{ slotIndex: 11, kind: "pending" }] : [])
            ];

            scenarios.forEach(scenario => {
                const prosumer = activeProsumers[(activeSequence * 11) % activeProsumers.length];
                reservations.push(createActiveReservation(
                    activeSequence,
                    station,
                    stationSlots[scenario.slotIndex],
                    prosumer,
                    scenario.kind,
                    backofficeId));
                activeSequence += 1;
            });
        }

        stationSlots.forEach(slot => {
            for (let historyIndex = 0; historyIndex < historyPerSlot; historyIndex += 1) {
                const status = [
                    ReservationStatus.Completed,
                    ReservationStatus.Cancelled,
                    ReservationStatus.Rejected
                ][(historySequence + historyIndex) % 3];
                const prosumer = prosumers[(historySequence * 13) % prosumers.length];
                reservations.push(createHistoricalReservation(
                    historySequence,
                    station,
                    slot,
                    prosumer,
                    status,
                    backofficeId,
                    operatorId));
                historySequence += 1;
            }
        });
    });

    return reservations;
}

// Recalculate the station's nested slots and availability count from standalone slots.
function synchronizeStations(stations, slotsByStation) {
    stations.forEach(station => {
        const stationSlots = slotsByStation.get(station._id);
        station.Slots = stationSlots.map(copySlot);
        station.TotalSlotCount = stationSlots.length;
        station.AvailableSlotCount = stationSlots.filter(slot => slot.Status === SlotStatus.Available).length;
        station.UpdatedAtUtc = stationSlots.reduce(
            (latest, slot) => slot.UpdatedAtUtc > latest ? slot.UpdatedAtUtc : latest,
            station.UpdatedAtUtc);
    });
}

// Ensure the same query and uniqueness indexes used by the API startup initializers.
function ensureIndexes() {
    collections.users.createIndex({ Email: 1 }, { name: "ux_users_email", unique: true });
    collections.users.createIndex({ Role: 1 }, { name: "ix_users_role" });
    collections.users.createIndex({ AccountStatus: 1 }, { name: "ix_users_account_status" });
    collections.prosumers.createIndex({ Email: 1 }, { name: "ux_prosumers_email", unique: true });
    collections.prosumers.createIndex({ AccountStatus: 1 }, { name: "ix_prosumers_account_status" });
    collections.stations.createIndex({ Code: 1 }, { name: "ux_solar_station_info_code", unique: true });
    collections.stations.createIndex({ Status: 1 }, { name: "ix_solar_station_info_status" });
    collections.stations.createIndex({ GeoLocation: "2dsphere" }, { name: "ix_solar_station_info_geo_location" });
    collections.slots.createIndex({ StationId: 1, SlotNumber: 1 }, { name: "ux_energy_booking_slots_station_slot_number", unique: true });
    collections.slots.createIndex({ StationId: 1 }, { name: "ix_energy_booking_slots_station_id" });
    collections.slots.createIndex({ StartTimeUtc: 1 }, { name: "ix_energy_booking_slots_start_time" });
    collections.slots.createIndex({ Status: 1 }, { name: "ix_energy_booking_slots_status" });
    collections.slots.createIndex({ StationId: 1, StartTimeUtc: 1, EndTimeUtc: 1 }, { name: "ix_energy_booking_slots_station_start_end" });
    collections.reservations.createIndex(
        { BookingSlotId: 1 },
        { name: "ux_energy_reservations_active_slot", unique: true, partialFilterExpression: { IsActiveForBookingSlot: true } });
    collections.reservations.createIndex(
        { ProsumerId: 1, ScheduledAtUtc: 1 },
        { name: "ix_energy_reservations_prosumer_scheduled_at" });
    collections.reservations.createIndex(
        { StationId: 1, Status: 1, ScheduledAtUtc: 1 },
        { name: "ix_energy_reservations_station_status_scheduled_at" });
    collections.reservations.createIndex(
        { Status: 1, ScheduledAtUtc: 1 },
        { name: "ix_energy_reservations_status_scheduled_at" });
}

// Delete only data owned by this script so repeated runs remain predictable.
function deletePreviousSeedData() {
    collections.reservations.deleteMany({
        _id: { $in: [
            new RegExp(`^${generatedIdPrefixes.activeReservation}`),
            new RegExp(`^${generatedIdPrefixes.historicalReservation}`)
        ] }
    });
    collections.slots.deleteMany({ _id: new RegExp(`^${generatedIdPrefixes.slot}`) });
    collections.stations.deleteMany({ _id: new RegExp(`^${generatedIdPrefixes.station}`) });
    collections.prosumers.deleteMany({ Email: /@solgridmail\.lk$/ });
    collections.users.deleteMany({ _id: new RegExp(`^${generatedIdPrefixes.user}`) });
}

// Insert a potentially large list in batches to stay friendly to local and hosted MongoDB servers.
function insertInBatches(collection, documents, batchSize = 500) {
    for (let index = 0; index < documents.length; index += batchSize) {
        collection.insertMany(documents.slice(index, index + batchSize), { ordered: true });
    }
}

// Select existing configured accounts for audit metadata, falling back to generated users.
function findActor(role, fallbackId) {
    return collections.users.findOne({
        Role: role,
        AccountStatus: AccountStatus.Active,
        _id: { $not: new RegExp(`^${generatedIdPrefixes.user}`) }
    }) || collections.users.findOne({ _id: fallbackId });
}

// Validate key cross-collection invariants before reporting success.
function validateSeed(stations, slots, reservations) {
    const stationIds = new Set(stations.map(item => item._id));
    const slotIds = new Set(slots.map(item => item._id));
    const activeSlotIds = new Set();

    reservations.forEach(reservation => {
        if (!stationIds.has(reservation.StationId) || !slotIds.has(reservation.BookingSlotId)) {
            throw new Error(`Reservation ${reservation._id} has a broken station or slot reference.`);
        }
        if (reservation.IsActiveForBookingSlot) {
            if (activeSlotIds.has(reservation.BookingSlotId)) {
                throw new Error(`Slot ${reservation.BookingSlotId} has more than one active reservation.`);
            }
            activeSlotIds.add(reservation.BookingSlotId);
        }
    });

    stations.forEach(station => {
        const expectedAvailable = station.Slots.filter(slot => slot.Status === SlotStatus.Available).length;
        if (station.AvailableSlotCount !== expectedAvailable || station.TotalSlotCount !== station.Slots.length) {
            throw new Error(`Station ${station._id} has inconsistent slot counts.`);
        }
    });
}

// Run the complete idempotent seed operation.
function runSeed() {
    ensureIndexes();
    deletePreviousSeedData();

    const users = createUsers();
    const prosumers = createProsumers();
    insertInBatches(collections.users, users);
    insertInBatches(collections.prosumers, prosumers);

    const backoffice = findActor(UserRole.Backoffice, createId(generatedIdPrefixes.user, 1));
    const operator = findActor(UserRole.GridOperator, createId(generatedIdPrefixes.user, 9));
    if (backoffice === null || operator === null) {
        throw new Error("An active Backoffice and Grid Operator account are required for reservation audit fields.");
    }

    const { stations, slots, slotsByStation } = createStationsAndSlots();
    const reservations = createReservations(
        stations,
        slotsByStation,
        prosumers,
        backoffice._id,
        operator._id);
    synchronizeStations(stations, slotsByStation);
    validateSeed(stations, slots, reservations);

    insertInBatches(collections.stations, stations);
    insertInBatches(collections.slots, slots);
    insertInBatches(collections.reservations, reservations);

    const existingConfiguredAccounts = collections.users.find({
        _id: { $not: new RegExp(`^${generatedIdPrefixes.user}`) }
    }, {
        _id: 1,
        Email: 1,
        Role: 1,
        AccountStatus: 1
    }).sort({ Role: 1, Email: 1 }).toArray();

    print(`Seeded database: ${databaseName}`);
    print(`Existing configured accounts preserved: ${existingConfiguredAccounts.length}`);
    print(`Seeded web users: ${users.length}`);
    print(`Prosumers: ${prosumers.length}`);
    print(`Solar stations: ${stations.length}`);
    print(`Booking slots: ${slots.length}`);
    print(`Reservations: ${reservations.length}`);
    print(`Active reservations: ${reservations.filter(item => item.IsActiveForBookingSlot).length}`);
    print("Seeded account password: SolGrid@2026!");
    print("Backoffice login: amaya.perera@solgrid.lk");
    print("Grid Operator login: nimal.ratnayake@solgrid.lk");
    print(`Example Prosumer login: ${prosumers.find(item => item.AccountStatus === ProsumerStatus.Active).Email}`);

    if (existingConfiguredAccounts.length > 0) {
        print("Configured accounts found (passwords are never displayed):");
        printjson(existingConfiguredAccounts);
    }
}

runSeed();
