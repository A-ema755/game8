# Tests

Test suites for Gene Forge using Unity Test Framework (NUnit).

## Structure
```
tests/
├── unit/           # Pure logic tests (damage calc, type chart, XP formulas)
├── integration/    # System interaction tests (combat flow, capture sequence)
├── performance/    # Benchmarks (A* pathfinding, mass damage calc)
└── playtest/       # Playtest session logs and feedback
```

## Running Tests
Unity Test Runner: Window > General > Test Runner
