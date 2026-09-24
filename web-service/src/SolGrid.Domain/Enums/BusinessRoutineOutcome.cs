/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: BusinessRoutineOutcome.cs
 * Description: Classifies a live check of one trading business rule.
 * Contributor: Dilshan Yapa
 */

namespace SolGrid.Domain.Enums;

public enum BusinessRoutineOutcome
{
    Clear = 1,
    Attention = 2,
    Breach = 3
}
