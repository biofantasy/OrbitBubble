using OrbitBubble.Core.Helpers;
using OrbitBubble.Core.Managers;
using OrbitBubble.Core.Repositories;
using OrbitBubble.Core.Services;

namespace OrbitBubble;

public class MainWindowDependencies {
  public IHotkeyManager HotkeyManager { get; }
  public IBubbleRepository BubbleRepository { get; }
  public GestureService GestureService { get; }
  public BubbleViewFactory BubbleViewFactory { get; }
  public BubbleLayoutService BubbleLayoutService { get; }
  public BubbleInteractionService BubbleInteractionService { get; }
  public BubbleValidationService BubbleValidationService { get; }
  public BubbleStateService BubbleStateService { get; }
  public MenuAnimationService MenuAnimationService { get; }
  public MenuFactory MenuFactory { get; }
  public WindowRuntimeService WindowRuntimeService { get; }
  public IGlobalMouseHook GlobalMouseHook { get; }

  public MainWindowDependencies(
    IHotkeyManager hotkeyManager,
    IBubbleRepository bubbleRepository,
    GestureService gestureService,
    BubbleViewFactory bubbleViewFactory,
    BubbleLayoutService bubbleLayoutService,
    BubbleInteractionService bubbleInteractionService,
    BubbleValidationService bubbleValidationService,
    BubbleStateService bubbleStateService,
    MenuAnimationService menuAnimationService,
    MenuFactory menuFactory,
    WindowRuntimeService windowRuntimeService,
    IGlobalMouseHook globalMouseHook) {
    HotkeyManager = hotkeyManager;
    BubbleRepository = bubbleRepository;
    GestureService = gestureService;
    BubbleViewFactory = bubbleViewFactory;
    BubbleLayoutService = bubbleLayoutService;
    BubbleInteractionService = bubbleInteractionService;
    BubbleValidationService = bubbleValidationService;
    BubbleStateService = bubbleStateService;
    MenuAnimationService = menuAnimationService;
    MenuFactory = menuFactory;
    WindowRuntimeService = windowRuntimeService;
    GlobalMouseHook = globalMouseHook;
  }
}
