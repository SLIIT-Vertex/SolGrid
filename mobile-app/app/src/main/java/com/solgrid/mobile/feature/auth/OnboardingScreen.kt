package com.solgrid.mobile.feature.auth

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.pager.HorizontalPager
import androidx.compose.foundation.pager.rememberPagerState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.solgrid.mobile.R
import com.solgrid.mobile.core.components.PrimaryButton
import com.solgrid.mobile.core.components.TextActionButton
import com.solgrid.mobile.core.design.AppType
import com.solgrid.mobile.core.design.SolGridTheme
import com.solgrid.mobile.core.design.Spacing
import kotlinx.coroutines.launch

private data class OnboardingPage(val image: Int, val title: String, val description: String)

private val pages = listOf(
    OnboardingPage(
        image = R.drawable.onboard_trading,
        title = "Trade Solar Energy Locally",
        description = "Find nearby microgrid nodes and book energy drop-off or charging slots in a few taps."
    ),
    OnboardingPage(
        image = R.drawable.onboard_analytics,
        title = "Track Every Booking",
        description = "See pending and approved reservations at a glance, with live status from the central grid."
    ),
    OnboardingPage(
        image = R.drawable.onboard_battery,
        title = "Complete With a Scan",
        description = "Show your secure transaction QR at the node — the Grid Operator verifies and finalizes your energy transfer instantly."
    )
)

@Composable
fun OnboardingScreen(onFinished: () -> Unit) {
    val colors = SolGridTheme.colors
    val pagerState = rememberPagerState(pageCount = { pages.size })
    val scope = androidx.compose.runtime.rememberCoroutineScope()

    Column(modifier = Modifier.fillMaxSize().background(colors.background)) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(Spacing.lg),
            horizontalArrangement = Arrangement.End
        ) {
            if (pagerState.currentPage < pages.lastIndex) {
                TextActionButton(text = "Skip", onClick = onFinished)
            }
        }

        HorizontalPager(state = pagerState, modifier = Modifier.weight(1f).fillMaxWidth()) { page ->
            OnboardingPageContent(pages[page])
        }

        Column(modifier = Modifier.padding(horizontal = Spacing.xxl)) {
            Row(
                modifier = Modifier.fillMaxWidth().padding(vertical = Spacing.lg),
                horizontalArrangement = Arrangement.Center
            ) {
                pages.indices.forEach { index ->
                    val selected = index == pagerState.currentPage
                    Box(
                        modifier = Modifier
                            .padding(horizontal = 4.dp)
                            .size(width = if (selected) 20.dp else 7.dp, height = 7.dp)
                            .clip(CircleShape)
                            .background(if (selected) colors.accent else colors.border)
                    )
                }
            }

            PrimaryButton(
                text = if (pagerState.currentPage == pages.lastIndex) "Get Started" else "Next",
                onClick = {
                    if (pagerState.currentPage == pages.lastIndex) {
                        onFinished()
                    } else {
                        scope.launch { pagerState.animateScrollToPage(pagerState.currentPage + 1) }
                    }
                },
                modifier = Modifier.fillMaxWidth().padding(bottom = Spacing.xxl)
            )
        }
    }
}

@Composable
private fun OnboardingPageContent(page: OnboardingPage) {
    val colors = SolGridTheme.colors
    Column(
        modifier = Modifier.fillMaxSize().padding(horizontal = Spacing.xxl),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Image(
            painter = painterResource(id = page.image),
            contentDescription = null,
            contentScale = ContentScale.Fit,
            modifier = Modifier.fillMaxWidth().aspectRatio(1.15f)
        )

        Text(
            page.title,
            style = AppType.screenTitle,
            color = colors.textPrimary,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(top = Spacing.xl)
        )
        Text(
            page.description,
            style = AppType.body,
            color = colors.textSecondary,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(top = Spacing.sm)
        )
    }
}
