package com.solgrid.mobile.core.navigation

/** Central route definitions for the two mobile roles: Solar Prosumer and Grid Operator. */
object Routes {
    const val SPLASH = "splash"
    const val ONBOARDING = "onboarding"
    const val LOGIN = "login"
    const val REGISTER = "register"

    // Prosumer
    const val PROSUMER_DASHBOARD = "prosumer_dashboard"
    const val PROSUMER_NODES_MAP = "prosumer_nodes_map"
    const val NODE_DETAIL = "node_detail/{nodeId}"
    fun nodeDetail(nodeId: String) = "node_detail/$nodeId"
    const val CREATE_RESERVATION = "create_reservation/{nodeId}"
    fun createReservation(nodeId: String) = "create_reservation/$nodeId"
    const val BOOKING_DETAIL = "booking_detail/{reservationId}"
    fun bookingDetail(reservationId: String) = "booking_detail/$reservationId"
    const val EDIT_RESERVATION = "edit_reservation/{reservationId}"
    fun editReservation(reservationId: String) = "edit_reservation/$reservationId"
    const val RESERVATION_SUMMARY = "reservation_summary/{reservationId}/{action}"
    fun reservationSummary(reservationId: String, action: String) = "reservation_summary/$reservationId/$action"
    const val BOOKINGS = "bookings"
    const val BOOKING_HISTORY = "booking_history"
    const val RESERVATION_QR = "reservation_qr/{reservationId}"
    fun reservationQr(reservationId: String) = "reservation_qr/$reservationId"
    const val PROSUMER_PROFILE = "prosumer_profile"
    const val EDIT_PROSUMER_PROFILE = "edit_prosumer_profile"
    const val DEACTIVATION_REQUEST = "deactivation_request"

    // Grid Operator
    const val OPERATOR_HOME = "operator_home"
    const val OPERATOR_NODES = "operator_nodes"
    const val OPERATOR_NODE_DETAIL = "operator_node/{nodeId}"
    fun operatorNodeDetail(nodeId: String) = "operator_node/$nodeId"
    const val OPERATOR_SCANNER = "operator_scanner"
    const val OPERATOR_VERIFICATION_RESULT = "operator_verification_result/{code}"
    fun operatorVerificationResult(code: String) = "operator_verification_result/$code"

    // Shared
    const val SETTINGS = "settings"
    const val HELP = "help"
}

/** Bottom navigation destinations for the Prosumer role. */
enum class ProsumerBottomNav(val route: String, val label: String) {
    DASHBOARD(Routes.PROSUMER_DASHBOARD, "Dashboard"),
    MAP(Routes.PROSUMER_NODES_MAP, "Nodes"),
    BOOKINGS(Routes.BOOKINGS, "Bookings"),
    PROFILE(Routes.PROSUMER_PROFILE, "Profile")
}
