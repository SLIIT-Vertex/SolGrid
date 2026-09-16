package com.solgrid.mobile.core.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.CloudOff
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.Inventory2
import androidx.compose.material.icons.outlined.Lock
import androidx.compose.material.icons.outlined.NotificationsNone
import androidx.compose.material.icons.outlined.SearchOff
import androidx.compose.material.icons.outlined.EventBusy
import androidx.compose.material.icons.outlined.UploadFile
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

/**
 * Shared layout for empty and error states: an icon in a soft circular tile, a title, a short
 * description, and an optional action button. No illustrations/robots — simple line icons only.
 */
@Composable
fun StatePlaceholder(
    icon: ImageVector,
    title: String,
    description: String,
    modifier: Modifier = Modifier,
    actionText: String? = null,
    onAction: (() -> Unit)? = null
) {
    val colors = SolGridTheme.colors
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(Spacing.xxl),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(
            modifier = Modifier
                .size(64.dp)
                .clip(CircleShape)
                .background(colors.surfaceAlt),
            contentAlignment = Alignment.Center
        ) {
            androidx.compose.material3.Icon(
                icon,
                contentDescription = null,
                tint = colors.textSecondary,
                modifier = Modifier.size(28.dp)
            )
        }
        Text(
            title,
            style = AppType.sectionTitle,
            color = colors.textPrimary,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(top = Spacing.lg)
        )
        Text(
            description,
            style = AppType.body,
            color = colors.textSecondary,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(top = Spacing.xs)
        )
        if (actionText != null && onAction != null) {
            SecondaryButton(
                text = actionText,
                onClick = onAction,
                modifier = Modifier.padding(top = Spacing.lg)
            )
        }
    }
}

object EmptyStateIcons {
    val NoActivity = Icons.Outlined.EventBusy
    val NoNotifications = Icons.Outlined.NotificationsNone
    val NoSavedItems = Icons.Outlined.Inventory2
    val NoSearchResults = Icons.Outlined.SearchOff
    val NoFiles = Icons.Outlined.UploadFile
}

/** Convenience wrapper for common empty states with sensible copy baked in. */
@Composable
fun EmptyState(
    kind: EmptyKind,
    modifier: Modifier = Modifier,
    actionText: String? = null,
    onAction: (() -> Unit)? = null
) {
    val (icon, title, desc) = when (kind) {
        EmptyKind.ACTIVITY -> Triple(EmptyStateIcons.NoActivity, "No activity yet", "Actions you take will show up here as a timeline.")
        EmptyKind.NOTIFICATIONS -> Triple(EmptyStateIcons.NoNotifications, "No notifications", "You're all caught up. New alerts will appear here.")
        EmptyKind.SAVED_ITEMS -> Triple(EmptyStateIcons.NoSavedItems, "No saved items", "Bookmark items to find them quickly later.")
        EmptyKind.SEARCH_RESULTS -> Triple(EmptyStateIcons.NoSearchResults, "No results found", "Try a different search term or adjust your filters.")
        EmptyKind.FILES -> Triple(EmptyStateIcons.NoFiles, "No files uploaded", "Files you upload will appear here.")
    }
    StatePlaceholder(icon, title, desc, modifier, actionText, onAction)
}

enum class EmptyKind { ACTIVITY, NOTIFICATIONS, SAVED_ITEMS, SEARCH_RESULTS, FILES }

enum class ErrorKind {
    NO_INTERNET, SERVER_ERROR, AUTH_FAILED, SESSION_EXPIRED, PERMISSION_DENIED, NOT_FOUND, UNKNOWN
}

@Composable
fun ErrorState(
    kind: ErrorKind,
    modifier: Modifier = Modifier,
    onRetry: (() -> Unit)? = null
) {
    val (icon, title, desc, actionText) = when (kind) {
        ErrorKind.NO_INTERNET -> ErrorCopy(Icons.Outlined.CloudOff, "No internet connection", "Check your connection and try again.", "Retry")
        ErrorKind.SERVER_ERROR -> ErrorCopy(Icons.Outlined.ErrorOutline, "Server error", "Something went wrong on our end. Please try again shortly.", "Retry")
        ErrorKind.AUTH_FAILED -> ErrorCopy(Icons.Outlined.Lock, "Authentication failed", "We couldn't verify your credentials. Please sign in again.", "Try again")
        ErrorKind.SESSION_EXPIRED -> ErrorCopy(Icons.Outlined.EventBusy, "Session expired", "For your security, please sign in again to continue.", "Sign in")
        ErrorKind.PERMISSION_DENIED -> ErrorCopy(Icons.Outlined.Lock, "Permission denied", "You don't have access to view this content.", "Go back")
        ErrorKind.NOT_FOUND -> ErrorCopy(Icons.Outlined.SearchOff, "Page not found", "The content you're looking for doesn't exist or was removed.", "Go back")
        ErrorKind.UNKNOWN -> ErrorCopy(Icons.Outlined.ErrorOutline, "Something went wrong", "An unexpected error occurred. Please try again.", "Retry")
    }
    StatePlaceholder(icon, title, desc, modifier, actionText, onRetry)
}

private data class ErrorCopy(
    val icon: ImageVector,
    val title: String,
    val description: String,
    val actionText: String
)
