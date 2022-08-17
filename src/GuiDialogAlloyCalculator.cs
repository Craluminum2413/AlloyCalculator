using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Cairo;
using System.Linq;
using System;

namespace AlloyCalculator
{
  internal sealed class GuiDialogAlloyCalculator : GuiDialog
  {
    public GuiDialogAlloyCalculator(ICoreClientAPI capi) : base(capi)
    {
      ComposeDialog();
      id = capi.World.RegisterGameTickListener(delegate
      {
        UpdateHelpText();
      }, 50);
    }

    private readonly long id;
    private string InputText { get; set; }
    private string NuggetsOutputText { get; set; }

    public override string ToggleKeyCombinationCode => "alloycalculator";
    private string TextInputError => $"{Lang.Get("alloycalculator:TextInputError")}";
    private string TextAlloy => $"{Lang.Get("alloycalculator:Alloy")}:";
    private string TextCurrent => $"{Lang.Get("alloycalculator:Current")}:";
    private string TextAlloyCalculator => Lang.Get("alloycalculator:Alloy Calculator");
    private string TextUnits => $"{Lang.Get("alloycalculator:Units")}:";
    private string TextLocked => $" ({Lang.Get("alloycalculator:Locked")})";
    private string TextRequiredNuggets => $"{Lang.Get("alloycalculator:Required nuggets")}";
    private string HoverTextUnits => Lang.Get("alloycalculator:Each nugget is equal to 5 units");
    private CairoFont BoldText => CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold);
    private CairoFont FontSize(int size) => CairoFont.WhiteDetailText().WithFontSize(size);

    private AlloyRecipe CurrentAlloyRecipe { get; set; }
    private GuiElementSlider LockedSlider { get; set; }

    private List<GuiElementSlider> Sliders => new()
    {
      SingleComposer.GetSlider("slider1"),
      SingleComposer.GetSlider("slider2"),
      SingleComposer.GetSlider("slider3"),
      SingleComposer.GetSlider("slider4")
    };

    private List<GuiElementDynamicText> PercentTexts => new()
    {
      SingleComposer.GetDynamicText("percent1"),
      SingleComposer.GetDynamicText("percent2"),
      SingleComposer.GetDynamicText("percent3"),
      SingleComposer.GetDynamicText("percent4")
    };

    private string ListOfAlloys
    {
      get
      {
        StringBuilder sb = new();

        foreach (var val in capi.GetMetalAlloys())
        {
          sb.AppendLine(val.Output.ResolvedItemstack.GetName());

          foreach (var ingred in val.Ingredients)
          {
            sb
              .Append("\t\t")
              .Append(ingred.ResolvedItemstack.GetName())
              .Append('\t')
              .AppendLine(Lang.Get("alloy-ratio-from-to", (int)(ingred.MinRatio * 100), (int)(ingred.MaxRatio * 100)));
          }
        }

        return sb.ToString();
      }
    }

    private string[] MetalAlloyCodes
    {
      get
      {
        var list = new List<string>();
        foreach (var val in capi.GetMetalAlloys()) list.Add(val.Output.ResolvedItemstack.Collectible.Code.ToString());
        return list.ToArray();
      }
    }

    private string[] MetalAlloyNames
    {
      get
      {
        var list = new List<string>();
        foreach (var val in capi.GetMetalAlloys()) list.Add(val.Output.ResolvedItemstack.GetName());
        return list.ToArray();
      }
    }

    private string GetRatiosText
    {
      get
      {
        var sb = new StringBuilder();
        sb
          .Append("\n[ ")
          .Append(string.Join(", ", GetRatios(CurrentAlloyRecipe).ToList().ConvertAll(x => x.ToString()).ToArray()))
          .Append(" ]");
        return sb.ToString();
      }
    }

    private void ComposeDialog()
    {
      ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterFixed);
      ElementBounds leftColumn = ElementBounds.Fixed(0, 50, 240, 500);
      ElementBounds rightColumn = leftColumn.RightCopy().FixedRightOf(leftColumn, 100);
      ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
      ElementBounds dropDownBounds = ElementBounds.Fixed(EnumDialogArea.CenterFixed, 120, 40, 240, 20);
      ElementBounds textInputBounds = ElementBounds.Fixed(EnumDialogArea.CenterFixed, dropDownBounds.fixedX - 300, dropDownBounds.fixedY, 80, 20);
      ElementBounds textAlloyBounds = ElementBounds.Fixed(dropDownBounds.Alignment, dropDownBounds.absFixedX, dropDownBounds.fixedY - 3, 120, dropDownBounds.fixedHeight);
      ElementBounds textUnitsBounds = ElementBounds.Fixed(textInputBounds.Alignment, textInputBounds.fixedX - 40, textInputBounds.fixedY - 3, 150, textInputBounds.fixedHeight);
      ElementBounds textErrorBounds = ElementBounds.Fixed(textUnitsBounds.Alignment, textUnitsBounds.fixedX + 60, textUnitsBounds.fixedY + 50, 250, textUnitsBounds.fixedHeight + 50);

      ElementBounds slider1Bounds = ElementBounds.Fixed(textUnitsBounds.Alignment, textUnitsBounds.fixedX, textErrorBounds.fixedY + 50, 120, textUnitsBounds.fixedHeight);
      ElementBounds slider2Bounds = ElementBounds.Fixed(textUnitsBounds.Alignment, textUnitsBounds.fixedX, slider1Bounds.fixedY + 50, 120, textUnitsBounds.fixedHeight);
      ElementBounds slider3Bounds = ElementBounds.Fixed(textUnitsBounds.Alignment, textUnitsBounds.fixedX, slider2Bounds.fixedY + 50, 120, textUnitsBounds.fixedHeight);
      ElementBounds slider4Bounds = ElementBounds.Fixed(textUnitsBounds.Alignment, textUnitsBounds.fixedX, slider3Bounds.fixedY + 50, 120, textUnitsBounds.fixedHeight);
      ElementBounds slider1TextBounds = ElementBounds.Fixed(textUnitsBounds.Alignment, slider1Bounds.fixedX + 180, slider1Bounds.fixedY - 3, 200, slider1Bounds.fixedHeight);
      ElementBounds slider2TextBounds = ElementBounds.Fixed(textUnitsBounds.Alignment, slider2Bounds.fixedX + 180, slider2Bounds.fixedY - 3, 200, slider2Bounds.fixedHeight);
      ElementBounds slider3TextBounds = ElementBounds.Fixed(textUnitsBounds.Alignment, slider3Bounds.fixedX + 180, slider3Bounds.fixedY - 3, 200, slider3Bounds.fixedHeight);
      ElementBounds slider4TextBounds = ElementBounds.Fixed(textUnitsBounds.Alignment, slider4Bounds.fixedX + 180, slider4Bounds.fixedY - 3, 200, slider4Bounds.fixedHeight);
      ElementBounds textNuggetsBounds = ElementBounds.Fixed(textUnitsBounds.Alignment, textUnitsBounds.fixedX + 180, slider4Bounds.fixedY + 50, 500, 500);

      bgBounds.BothSizing = ElementSizing.FitToChildren;
      bgBounds.WithChildren(leftColumn, rightColumn);
      SingleComposer = capi.Gui.CreateCompo("alloycalculator", dialogBounds)
      .AddShadedDialogBG(bgBounds)
      .AddDialogTitleBar(TextAlloyCalculator, OnTitleBarCloseClicked, BoldText)
      .AddStaticText(TextAlloy, FontSize(18), textAlloyBounds, "dropdown_description")
      .AddDropDown(MetalAlloyCodes, MetalAlloyNames, 0, (newval, on) => UpdateDropDown(newval), dropDownBounds, "dropdown")
      .AddStaticText(TextUnits, FontSize(18), textUnitsBounds, "textinput_description")
      .AddAutoSizeHoverText(HoverTextUnits, FontSize(18), 300, textUnitsBounds, "units_hovertext")
      .AddTextInput(textInputBounds, OnTextChanged, null, "textinput")
      .AddDynamicText("", FontSize(13), rightColumn, "alloys")
      .AddSlider(null, slider1Bounds, "slider1")
      .AddSlider(null, slider2Bounds, "slider2")
      .AddSlider(null, slider3Bounds, "slider3")
      .AddSlider(null, slider4Bounds, "slider4")
      .AddDynamicText("", FontSize(18), slider1TextBounds, "percent1")
      .AddDynamicText("", FontSize(18), slider2TextBounds, "percent2")
      .AddDynamicText("", FontSize(18), slider3TextBounds, "percent3")
      .AddDynamicText("", FontSize(18), slider4TextBounds, "percent4")
      .AddDynamicText("", FontSize(18), textNuggetsBounds, "nuggets")
      .AddDynamicText("", FontSize(16), textErrorBounds, "error_text")
      .Compose();

      UpdateDropDown(SingleComposer.GetDropDown("dropdown").SelectedValue);
    }

    private AlloyRecipe GetAlloyRecipe(string alloyCode)
    {
      {
        foreach (var val in capi.GetMetalAlloys())
        {
          if (val.Output.Code.ToString() == alloyCode) return val;
        }
      }
      return (AlloyRecipe)Enumerable.Empty<AlloyRecipe>();
    }

    private int GetAverage(int min, int max) => (min + max) / 2;

    private void SetSlidersOnce()
    {
      for (int i = 0; i < 4; ++i)
      {
        var slider = SingleComposer.GetSlider($"slider{i + 1}");

        if (i < CurrentAlloyRecipe.Ingredients.Length)
        {
          var min = (int)(CurrentAlloyRecipe.Ingredients[i].MinRatio * 100);
          var max = (int)(CurrentAlloyRecipe.Ingredients[i].MaxRatio * 100);
          slider.SetValues(GetAverage(min, max), min, max, 1);
        }
        else
        {
          slider.SetValues(0, 0, 1, 1);
        }
        slider.Enabled = i < CurrentAlloyRecipe.Ingredients.Length;
      }
    }

    private string IsLocked(GuiElementSlider slider) => slider == LockedSlider ? TextLocked : "";

    private void TryAdjustSliders()
    {
      // Clear previous values
      foreach (var percent in PercentTexts) percent.SetNewText("");
      foreach (var slider in Sliders) slider.Enabled = true;

      if (CurrentAlloyRecipe.Ingredients.Length is 2)
      {
        Sliders[1].Enabled = false;
        LockedSlider = Sliders[1];
        PercentTexts[0].SetNewText($"{Sliders[0].GetValue()}%{IsLocked(Sliders[0])}");
        PercentTexts[1].SetNewText($"{Sliders[1].GetValue()}%{IsLocked(Sliders[1])}");
      }

      if (CurrentAlloyRecipe.Ingredients.Length is 3)
      {
        Sliders[2].Enabled = false;
        LockedSlider = Sliders[2];
        PercentTexts[0].SetNewText($"{Sliders[0].GetValue()}%{IsLocked(Sliders[0])}");
        PercentTexts[1].SetNewText($"{Sliders[1].GetValue()}%{IsLocked(Sliders[1])}");
        PercentTexts[2].SetNewText($"{Sliders[2].GetValue()}%{IsLocked(Sliders[2])}");
      }

      if (CurrentAlloyRecipe.Ingredients.Length is 4)
      {
        Sliders[3].Enabled = false;
        LockedSlider = Sliders[3];
        PercentTexts[0].SetNewText($"{Sliders[0].GetValue()}%{IsLocked(Sliders[0])}");
        PercentTexts[1].SetNewText($"{Sliders[1].GetValue()}%{IsLocked(Sliders[1])}");
        PercentTexts[2].SetNewText($"{Sliders[2].GetValue()}%{IsLocked(Sliders[2])}");
        PercentTexts[3].SetNewText($"{Sliders[3].GetValue()}%{IsLocked(Sliders[3])}");
      }

      var first = Sliders[0].GetValue();
      var second = Sliders[1].GetValue();
      var third = Sliders[2].GetValue();
      var fourth = Sliders[3].GetValue();

      if (CurrentAlloyRecipe.Ingredients.Length is 2
      && first + second is < 100 or > 100)
      {
        LockedSlider.SetValue(100 - first);
      }

      if (CurrentAlloyRecipe.Ingredients.Length is 3
      && first + second + third is < 100 or > 100)
      {
        LockedSlider.SetValue(100 - first - second);
      }

      if (CurrentAlloyRecipe.Ingredients.Length is 4
      && first + second + third + fourth is < 100 or > 100)
      {
        LockedSlider.SetValue(100 - first - second - third);
      }

      SingleComposer.ReCompose();
    }

    private IEnumerable<int> GetRatios(AlloyRecipe recipe)
    {
      return recipe?.Ingredients?.Select(ingredient =>
      {
        var min = (int)(ingredient.MinRatio * 100);
        var max = (int)(ingredient.MaxRatio * 100);
        return GetAverage(min, max);
      }) ?? Enumerable.Empty<int>();
    }

    private void UpdateDropDown(string value)
    {
      CurrentAlloyRecipe = GetAlloyRecipe(value);
      Array.Sort(CurrentAlloyRecipe.Ingredients, (ing1, ing2) => ing1.MaxRatio.CompareTo(ing2.MaxRatio));
      SetSlidersOnce();
    }

    private void UpdateHelpText()
    {
      string alloyCode = CurrentAlloyRecipe.Output.Code.ToString();

      SingleComposer
      .GetDynamicText("alloys")
      .SetNewText($"{TextCurrent} {alloyCode}{GetRatiosText}\n\n{ListOfAlloys}");

      SingleComposer.GetDynamicText("nuggets").SetNewText(NuggetsOutputText ?? "");

      if (!TryCalculate(InputText))
      {
        SingleComposer.GetDynamicText("error_text").SetNewText(TextInputError);
      }
      else
      {
        SingleComposer.GetDynamicText("error_text").SetNewText("");
      }

      TryCalculate(InputText);
      TryAdjustSliders();
    }

    private void OnTextChanged(string text) => InputText = text;

    private bool TryCalculate(string text)
    {
      List<int> output = new();
      bool isint = int.TryParse(text, out int input);

      if (!isint || input % 5 != 0 || CurrentAlloyRecipe is null || input < 100)
      {
        InputText = TextInputError;
        NuggetsOutputText = "";
        return false;
      }

      float totalNuggets = (float)input / 100 * 20;
      int sumOfNuggets = 0;
      for (int i = 0; i < Sliders.Count; i++)
      {
        int nuggets;
        if (i < Sliders.Count - 1)
        {
          int percentage = Sliders[i].GetValue();
          nuggets = (int)Math.Round(percentage * totalNuggets / 100);
          sumOfNuggets += nuggets;
        }
        else
        {
          nuggets = (int)totalNuggets - sumOfNuggets;
        }
        output.Add(nuggets);
      }

      GetNuggetsOutput(output.ToList());
      return true;
    }

    private string GetNuggetsOutput(List<int> input)
    {
      StringBuilder sb = new();

      sb.Append(TextRequiredNuggets).AppendLine(":").AppendLine();

      for (int j = 0; j < CurrentAlloyRecipe.Ingredients.Length; j++)
      {
        var variant = CurrentAlloyRecipe.Ingredients[j].Code.EndVariant();
        sb.Append(Lang.Get("material-" + variant));
        sb.Append(":\t").Append(input[j]).AppendLine();
      }

      NuggetsOutputText = sb.ToString();
      return sb.ToString();
    }

    private void OnTitleBarCloseClicked() => TryClose();

    public override void Dispose()
    {
      base.Dispose();
      capi.World.UnregisterGameTickListener(id);
    }
  }
}