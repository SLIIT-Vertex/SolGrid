package com.solgrid.mobile.core.navigation

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.solgrid.mobile.core.components.ProsumerBottomNavigation
import com.solgrid.mobile.core.design.AppThemeMode
import com.solgrid.mobile.feature.auth.AuthViewModel
import com.solgrid.mobile.feature.auth.LoginScreen
import com.solgrid.mobile.feature.auth.OnboardingScreen
import com.solgrid.mobile.feature.auth.RegisterScreen
import com.solgrid.mobile.feature.auth.SplashScreen
import com.solgrid.mobile.core.models.AppRole
import com.solgrid.mobile.core.network.SessionStore
import com.solgrid.mobile.feature.operator.OperatorHomeScreen
import com.solgrid.mobile.feature.operator.OperatorViewModel
import com.solgrid.mobile.feature.operator.ScannerScreen
import com.solgrid.mobile.feature.operator.VerificationResultScreen
import com.solgrid.mobile.feature.prosumer.BookingHistoryScreen
import com.solgrid.mobile.feature.prosumer.BookingsScreen
import com.solgrid.mobile.feature.prosumer.CreateReservationScreen
import com.solgrid.mobile.feature.prosumer.DashboardScreen
import com.solgrid.mobile.feature.prosumer.DeactivationRequestScreen
import com.solgrid.mobile.feature.prosumer.EditProsumerProfileScreen
import com.solgrid.mobile.feature.prosumer.EditReservationScreen
import com.solgrid.mobile.feature.prosumer.NodeDetailScreen
import com.solgrid.mobile.feature.prosumer.NodesMapScreen
import com.solgrid.mobile.feature.prosumer.ProsumerProfileScreen
import com.solgrid.mobile.feature.prosumer.ProsumerViewModel
import com.solgrid.mobile.feature.prosumer.ReservationQrScreen
import com.solgrid.mobile.feature.prosumer.ReservationSummaryScreen
import com.solgrid.mobile.feature.prosumer.SummaryAction
import com.solgrid.mobile.feature.shared.HelpScreen
import com.solgrid.mobile.feature.shared.SettingsScreen
import com.solgrid.mobile.feature.microgrid.NodeViewModel
import com.solgrid.mobile.feature.operator.OperatorNodesScreen
import com.solgrid.mobile.feature.operator.OperatorNodeDetailScreen

private val prosumerBottomNavRoutes = ProsumerBottomNav.entries.map { it.route }.toSet()

@Composable
fun AppNavGraph(onThemeModeChange: (AppThemeMode) -> Unit) {
    val navController = rememberNavController()

    val authViewModel = remember { AuthViewModel() }
    val prosumerViewModel = remember { ProsumerViewModel() }
    val operatorViewModel = remember { OperatorViewModel() }
    val nodeViewModel = remember { NodeViewModel() }

    val backStackEntry by navController.currentBackStackEntryAsState()
    val currentRoute = backStackEntry?.destination?.route
    val showBottomBar = currentRoute in prosumerBottomNavRoutes

    Scaffold(
        bottomBar = {
            if (showBottomBar) {
                val currentDest = ProsumerBottomNav.entries.find { it.route == currentRoute } ?: ProsumerBottomNav.DASHBOARD
                ProsumerBottomNavigation(current = currentDest) { dest ->
                    navController.navigate(dest.route) {
                        popUpTo(Routes.PROSUMER_DASHBOARD) { saveState = true }
                        launchSingleTop = true
                        restoreState = true
                    }
                }
            }
        }
    ) { padding ->
        Box(modifier = Modifier.fillMaxSize()) {
            NavHost(
                navController = navController,
                startDestination = Routes.SPLASH,
                modifier = Modifier
                    .fillMaxSize()
                    .padding(bottom = if (showBottomBar) padding.calculateBottomPadding() else 0.dp)
            ) {
                composable(Routes.SPLASH) {
                    SplashScreen(onFinished = {
                        val destination = when {
                            SessionStore.accessToken == null -> Routes.ONBOARDING
                            SessionStore.role == "GridOperator" -> Routes.OPERATOR_HOME
                            SessionStore.role == "Prosumer" -> {
                                prosumerViewModel.loadProfile()
                                Routes.PROSUMER_DASHBOARD
                            }
                            else -> Routes.ONBOARDING
                        }
                        navController.navigate(destination) { popUpTo(Routes.SPLASH) { inclusive = true } }
                    })
                }
                composable(Routes.ONBOARDING) {
                    OnboardingScreen(onFinished = {
                        navController.navigate(Routes.LOGIN) { popUpTo(Routes.ONBOARDING) { inclusive = true } }
                    })
                }
                composable(Routes.LOGIN) {
                    LoginScreen(
                        viewModel = authViewModel,
                        onLoginSuccess = { role ->
                            val destination = if (role == AppRole.PROSUMER) {
                                prosumerViewModel.loadProfile()
                                Routes.PROSUMER_DASHBOARD
                            } else {
                                Routes.OPERATOR_HOME
                            }
                            navController.navigate(destination) { popUpTo(0) { inclusive = true } }
                        },
                        onNavigateToRegister = { navController.navigate(Routes.REGISTER) }
                    )
                }
                composable(Routes.REGISTER) {
                    RegisterScreen(
                        viewModel = authViewModel,
                        onRegisterSuccess = {
                            // New accounts start Pending until Backoffice activates them, so
                            // route back to Login rather than straight into the dashboard.
                            navController.navigate(Routes.LOGIN) { popUpTo(Routes.LOGIN) { inclusive = true } }
                        },
                        onNavigateToLogin = { navController.popBackStack() }
                    )
                }

                // ---- Prosumer ----
                composable(Routes.PROSUMER_DASHBOARD) {
                    DashboardScreen(
                        viewModel = prosumerViewModel,
                        onProfileClick = { navController.navigate(Routes.PROSUMER_PROFILE) },
                        onFindNodesClick = { navController.navigate(Routes.PROSUMER_NODES_MAP) },
                        onBookingClick = { id -> navController.navigate(Routes.editReservation(id)) },
                        onViewAllBookings = { navController.navigate(Routes.BOOKINGS) }
                    )
                }
                composable(Routes.PROSUMER_NODES_MAP) {
                    NodesMapScreen(
                        viewModel = nodeViewModel,
                        onBack = { navController.popBackStack() },
                        onNodeClick = { id -> navController.navigate(Routes.nodeDetail(id)) }
                    )
                }
                composable(
                    Routes.NODE_DETAIL,
                    arguments = listOf(navArgument("nodeId") { type = NavType.StringType })
                ) { entry ->
                    val nodeId = entry.arguments?.getString("nodeId").orEmpty()
                    NodeDetailScreen(
                        viewModel = nodeViewModel,
                        nodeId = nodeId,
                        onBack = { navController.popBackStack() },
                        onBookSlot = { id -> navController.navigate(Routes.createReservation(id)) }
                    )
                }
                composable(
                    Routes.CREATE_RESERVATION,
                    arguments = listOf(navArgument("nodeId") { type = NavType.StringType })
                ) { entry ->
                    val nodeId = entry.arguments?.getString("nodeId").orEmpty()
                    CreateReservationScreen(
                        viewModel = prosumerViewModel,
                        nodeId = nodeId,
                        onBack = { navController.popBackStack() },
                        onCreated = { id ->
                            navController.navigate(Routes.reservationSummary(id, "CREATED")) {
                                popUpTo(Routes.PROSUMER_DASHBOARD)
                            }
                        }
                    )
                }
                composable(
                    Routes.EDIT_RESERVATION,
                    arguments = listOf(navArgument("reservationId") { type = NavType.StringType })
                ) { entry ->
                    val reservationId = entry.arguments?.getString("reservationId").orEmpty()
                    EditReservationScreen(
                        viewModel = prosumerViewModel,
                        reservationId = reservationId,
                        onBack = { navController.popBackStack() },
                        onUpdated = {
                            navController.navigate(Routes.reservationSummary(reservationId, "UPDATED")) {
                                popUpTo(Routes.PROSUMER_DASHBOARD)
                            }
                        },
                        onCancelled = {
                            navController.navigate(Routes.reservationSummary(reservationId, "CANCELLED")) {
                                popUpTo(Routes.PROSUMER_DASHBOARD)
                            }
                        }
                    )
                }
                composable(
                    Routes.RESERVATION_SUMMARY,
                    arguments = listOf(
                        navArgument("reservationId") { type = NavType.StringType },
                        navArgument("action") { type = NavType.StringType }
                    )
                ) { entry ->
                    val action = SummaryAction.valueOf(entry.arguments?.getString("action") ?: "CREATED")
                    ReservationSummaryScreen(
                        action = action,
                        onViewBookings = {
                            navController.navigate(Routes.BOOKINGS) { popUpTo(Routes.PROSUMER_DASHBOARD) }
                        },
                        onBackToDashboard = {
                            navController.navigate(Routes.PROSUMER_DASHBOARD) { popUpTo(Routes.PROSUMER_DASHBOARD) { inclusive = true } }
                        }
                    )
                }
                composable(Routes.BOOKINGS) {
                    BookingsScreen(
                        viewModel = prosumerViewModel,
                        onBack = { navController.popBackStack() },
                        onBookingClick = { id -> navController.navigate(Routes.editReservation(id)) }
                    )
                }
                composable(Routes.BOOKING_HISTORY) {
                    BookingHistoryScreen(
                        viewModel = prosumerViewModel,
                        onBack = { navController.popBackStack() },
                        onBookingClick = { id -> navController.navigate(Routes.editReservation(id)) }
                    )
                }
                composable(
                    Routes.RESERVATION_QR,
                    arguments = listOf(navArgument("reservationId") { type = NavType.StringType })
                ) { entry ->
                    val reservationId = entry.arguments?.getString("reservationId").orEmpty()
                    ReservationQrScreen(viewModel = prosumerViewModel, reservationId = reservationId, onBack = { navController.popBackStack() })
                }
                composable(Routes.PROSUMER_PROFILE) {
                    ProsumerProfileScreen(
                        viewModel = prosumerViewModel,
                        onEditProfile = { navController.navigate(Routes.EDIT_PROSUMER_PROFILE) },
                        onDeactivationRequest = { navController.navigate(Routes.DEACTIVATION_REQUEST) },
                        onSettings = { navController.navigate(Routes.SETTINGS) },
                        onHelp = { navController.navigate(Routes.HELP) },
                        onLogout = {
                            SessionStore.clear()
                            nodeViewModel.clear()
                            authViewModel.resetLoginForm()
                            navController.navigate(Routes.LOGIN) { popUpTo(0) { inclusive = true } }
                        }
                    )
                }
                composable(Routes.EDIT_PROSUMER_PROFILE) {
                    EditProsumerProfileScreen(viewModel = prosumerViewModel, onBack = { navController.popBackStack() })
                }
                composable(Routes.DEACTIVATION_REQUEST) {
                    DeactivationRequestScreen(
                        viewModel = prosumerViewModel,
                        onBack = { navController.popBackStack() },
                        onDeactivated = { navController.popBackStack() }
                    )
                }

                // ---- Grid Operator ----
                composable(Routes.OPERATOR_HOME) {
                    OperatorHomeScreen(
                        viewModel = operatorViewModel,
                        onScanClick = { navController.navigate(Routes.OPERATOR_SCANNER) },
                        onBookingClick = { },
                        onNodesClick = { navController.navigate(Routes.OPERATOR_NODES) },
                        onLogout = {
                            SessionStore.clear()
                            nodeViewModel.clear()
                            authViewModel.resetLoginForm()
                            navController.navigate(Routes.LOGIN) { popUpTo(0) { inclusive = true } }
                        }
                    )
                }
                composable(Routes.OPERATOR_NODES) {
                    OperatorNodesScreen(nodeViewModel, onBack = { navController.popBackStack() }, onNodeClick = { id -> navController.navigate(Routes.operatorNodeDetail(id)) })
                }
                composable(Routes.OPERATOR_NODE_DETAIL, arguments = listOf(navArgument("nodeId") { type = NavType.StringType })) { entry ->
                    OperatorNodeDetailScreen(nodeViewModel, entry.arguments?.getString("nodeId").orEmpty(), onBack = { navController.popBackStack() })
                }
                composable(Routes.OPERATOR_SCANNER) {
                    ScannerScreen(
                        onBack = { navController.popBackStack() },
                        onCodeScanned = { code -> navController.navigate(Routes.operatorVerificationResult(code)) }
                    )
                }
                composable(
                    Routes.OPERATOR_VERIFICATION_RESULT,
                    arguments = listOf(navArgument("code") { type = NavType.StringType })
                ) { entry ->
                    val code = entry.arguments?.getString("code").orEmpty()
                    VerificationResultScreen(
                        viewModel = operatorViewModel,
                        code = code,
                        onDone = { navController.navigate(Routes.OPERATOR_HOME) { popUpTo(Routes.OPERATOR_HOME) { inclusive = true } } },
                        onScanAnother = { navController.popBackStack(Routes.OPERATOR_SCANNER, inclusive = false) }
                    )
                }

                // ---- Shared ----
                composable(Routes.SETTINGS) {
                    SettingsScreen(onBack = { navController.popBackStack() }, onThemeModeChange = onThemeModeChange)
                }
                composable(Routes.HELP) {
                    HelpScreen(onBack = { navController.popBackStack() })
                }
            }
        }
    }
}
