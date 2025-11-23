# Tech Stack

## Unity
--**Input system**: We are using the new input system on all the project

### Version & Platforms
- **Unity Version**: 6000.2.10f1
- **Target Platforms**: Windows
- **Project Type**: Idle cosy game

### Architecture
- **Graphics**: All the game graphics will be made with UI and canvas.

## Tools, Frameworks & Third Party Dependencies

### Animation & UI
- **Spine** - 2D skeletal animation system for character animations
- **DOTween** - Animation framework for tweening and UI animations

### Async & Performance
- **UniTask** - Async/await support for Unity, replacing coroutines
- **UniTask.DOTween** - DOTween integration for async animations

### Development & Debugging
 -**Debugging** - For debugging we use the DebugManager so that we can enable and disable the diferent logs. Every log should include his type

### Unity Bridge Server
External tool communication system

#### Claude instructions (IMPORTANT)
- Use Bridge always to verify that your code changes compile, fix possible errors
- Use Bridge always to verify that your generated test compile and test go through
