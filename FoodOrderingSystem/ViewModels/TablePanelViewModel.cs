namespace FoodOrderingSystem.ViewModels;

// Data for the shared table panel (Pages/Staff/_TablePanel.cshtml): the chosen table and the dining area on screen,
// so the panel's buttons and close link come back to the same area.
public record TablePanelViewModel(FloorTableViewModel Table, int? AreaId);
