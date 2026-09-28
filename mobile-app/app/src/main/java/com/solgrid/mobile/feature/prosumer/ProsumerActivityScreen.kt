package com.solgrid.mobile.feature.prosumer

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.SecondaryButton
import com.solgrid.mobile.core.components.AppDivider
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import com.solgrid.mobile.core.network.ProsumerRepository
import com.solgrid.mobile.core.network.ProsumerActivityDto
import com.solgrid.mobile.core.network.PagedResponseDto
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter

@Composable
fun ProsumerActivityScreen(onBack: () -> Unit) {
    val colors = SolGridTheme.colors
    val repository = remember { ProsumerRepository() }
    var page by remember { mutableIntStateOf(1) }
    var retry by remember { mutableIntStateOf(0) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var history by remember { mutableStateOf<PagedResponseDto<ProsumerActivityDto>?>(null) }
    LaunchedEffect(page, retry) {
        loading = true
        error = null
        repository.getActivity(page).onSuccess { history = it }.onFailure { error = it.message }
        loading = false
    }
    Column(Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Account History", onBack = onBack)
        Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(Spacing.lg)) {
            when {
                loading -> Text("Loading account history…", color = colors.textSecondary)
                error != null -> {
                    Text(error.orEmpty(), color = colors.error)
                    SecondaryButton(text = "Retry", onClick = { retry++ })
                }
                else -> history?.let { result ->
                    if (result.items.isEmpty()) Text("No recorded activity yet. Older changes were not recorded.", color = colors.textSecondary)
                    result.items.forEach { event ->
                        val title = when (event.action) {
                            "Registered" -> "Account registered"
                            "ProfileUpdated" -> "Profile updated"
                            "Activated" -> "Account activated"
                            "DeactivationRequested" -> "Deactivation requested"
                            "Deactivated" -> "Account deactivated"
                            "Reactivated" -> "Account reactivated"
                            else -> event.action
                        }
                        Text(title, style = AppType.bodyStrong, color = colors.textPrimary, modifier = Modifier.padding(top = Spacing.md))
                        val date = runCatching {
                            OffsetDateTime.parse(event.occurredAt)
                                .atZoneSameInstant(ZoneId.systemDefault())
                                .format(DateTimeFormatter.ofPattern("dd MMM yyyy, HH:mm"))
                        }.getOrDefault(event.occurredAt)
                        Text("$date · ${event.actorRole}", style = AppType.supporting, color = colors.textSecondary)
                        event.reason?.let { Text(it, style = AppType.body, color = colors.textPrimary, modifier = Modifier.padding(vertical = Spacing.sm)) }
                        AppDivider()
                    }
                    Row(horizontalArrangement = Arrangement.spacedBy(Spacing.md), modifier = Modifier.padding(top = Spacing.lg)) {
                        SecondaryButton(text = "Previous", enabled = page > 1, onClick = { page-- })
                        SecondaryButton(text = "Next", enabled = result.pageNumber.toLong() * result.pageSize < result.totalCount, onClick = { page++ })
                    }
                }
            }
        }
    }
}
