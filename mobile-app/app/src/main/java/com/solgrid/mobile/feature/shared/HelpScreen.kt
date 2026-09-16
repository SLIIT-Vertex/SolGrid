package com.solgrid.mobile.feature.shared

import androidx.compose.animation.animateContentSize
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.ExpandLess
import androidx.compose.material.icons.outlined.ExpandMore
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import com.solgrid.mobile.core.components.AppTopBar
import com.solgrid.mobile.core.components.SectionHeader
import com.solgrid.mobile.core.components.clickableNoRipple
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.Radius
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing

private data class Faq(val question: String, val answer: String)

private val faqs = listOf(
    Faq("How far ahead can I book a slot?", "Reservations can only be scheduled within the next 7 days, enforced by the central server."),
    Faq("How late can I change or cancel a booking?", "Updates and cancellations require at least 12 hours notice before the booking's start time."),
    Faq("What happens if I deactivate my account?", "Your account is deactivated immediately. Only a Backoffice officer can reactivate it."),
    Faq("How does the transaction QR work?", "Once your reservation is approved, a secure QR is generated. Show it to the Grid Operator at the node to finalize the transfer.")
)

@Composable
fun HelpScreen(onBack: () -> Unit) {
    val colors = SolGridTheme.colors
    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        AppTopBar(title = "Help & Support", onBack = onBack)
        Column(modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(horizontal = Spacing.lg)) {
            SectionHeader(title = "Frequently Asked Questions", modifier = Modifier.padding(top = Spacing.md))
            faqs.forEach { FaqItem(it) }
        }
    }
}

@Composable
private fun FaqItem(faq: Faq) {
    val colors = SolGridTheme.colors
    var expanded by remember { mutableStateOf(false) }
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = Spacing.xs)
            .clip(RoundedCornerShape(Radius.md))
            .background(colors.surface)
            .animateContentSize()
    ) {
        Row(
            modifier = Modifier.fillMaxWidth().clickableNoRipple { expanded = !expanded }.padding(Spacing.md),
            horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(faq.question, style = AppType.bodyStrong, color = colors.textPrimary, modifier = Modifier.weight(1f))
            Icon(if (expanded) Icons.Outlined.ExpandLess else Icons.Outlined.ExpandMore, contentDescription = null, tint = colors.textSecondary)
        }
        if (expanded) {
            Text(faq.answer, style = AppType.body, color = colors.textSecondary, modifier = Modifier.padding(start = Spacing.md, end = Spacing.md, bottom = Spacing.md))
        }
    }
}
