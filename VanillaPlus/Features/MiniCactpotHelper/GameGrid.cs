using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Nodes;

namespace VanillaPlus.MiniCactpotHelper;

public class GameGrid : ResNode {

	private readonly IconImageNode[] buttonImages;
	private readonly ImageNode[] laneImages;

	public GameGrid(MiniCactpotHelperConfig config) {
		var gameGridOffset = new Vector2(28.0f, 44.0f);
		var buttonsOffset = gameGridOffset + new Vector2(32.0f, 32.0f);

		AddTimeline(new TimelineBuilder()
			.BeginFrameSet(1, 130)
			.AddLabel(1, 200, AtkTimelineJumpBehavior.Start, 0)
			.AddLabel(120, 0, AtkTimelineJumpBehavior.LoopForever, 200)
			.AddLabel(121, 201, AtkTimelineJumpBehavior.Start, 0)
			.EndFrameSet()
			.Build());
		
		buttonImages = new IconImageNode[9];
		
		foreach(var yIndex in Enumerable.Range(0, 3))
		foreach (var xIndex in Enumerable.Range(0, 3)) {
			var imageNode = new IconImageNode {
				Position = new Vector2(54.0f * xIndex, 54.0f * yIndex - 1.0f) + buttonsOffset,
				Size = new Vector2(54.0f, 54.0f),
				Origin = new Vector2(27.0f, 27.0f),
				Scale = new Vector2(0.8f, 0.8f),
				IsVisible = true,
			};

			imageNode.AddTimeline(new TimelineBuilder()
				.BeginFrameSet(1, 120)
				.AddFrame(1, scale: new Vector2(0.8f, 0.8f), rotation: 0.0f)
				.AddFrame(60, scale: new Vector2(0.7f, 0.7f), rotation: MathF.PI)
				.AddFrame(120, scale: new Vector2(0.8f, 0.8f), rotation: 2.0f * MathF.PI)
				.EndFrameSet()
				.BeginFrameSet(121, 130)
				.AddFrame(121, scale: new Vector2(0.8f, 0.8f), rotation: 0.0f)
				.EndFrameSet()
				.Build());
			
			buttonImages[xIndex + yIndex * 3] = imageNode;
			
			System.NativeController.AttachNode(imageNode, this);
		}
		
		laneImages = new ImageNode[8];

		foreach (var index in Enumerable.Range(0, 3)) {
			laneImages[index] = new IconImageNode {
				Position = new Vector2(0.0f, 40.0f + 54.0f * index) + gameGridOffset,
				Size = new Vector2(34.0f, 34.0f),
				Origin = new Vector2(17.0f, 17.0f),
				IsVisible = true,
				IconId = 60934,
			};
			
			AddLaneNodeTimeline(laneImages[index], MathF.PI / 2.0f);
            System.NativeController.AttachNode(laneImages[index], this);
		}

		foreach (var index in Enumerable.Range(0, 3)) {
			var arrayIndex = index + 3;
			
			laneImages[arrayIndex] = new IconImageNode {
				Position = new Vector2(42.0f + 54.0f * index, 0.0f) + gameGridOffset,
				Size = new Vector2(34.0f, 34.0f),
				Origin = new Vector2(17.0f, 17.0f),
				IsVisible = true,
				IconId = 60934,
			};
			
			AddLaneNodeTimeline(laneImages[arrayIndex], MathF.PI);
            System.NativeController.AttachNode(laneImages[arrayIndex], this);
		}
		
		laneImages[6] = new IconImageNode {
			Position = new Vector2(0.0f, 0.0f) + gameGridOffset,
			Size = new Vector2(34.0f, 34.0f),
			Origin = new Vector2(17.0f, 17.0f),
			IsVisible = true,
			IconId = 60934,
		};
		
		AddLaneNodeTimeline(laneImages[6], MathF.PI * 3.0f / 4.0f);
        System.NativeController.AttachNode(laneImages[6], this);
		
		laneImages[7] = new IconImageNode {
			Position = new Vector2(190.0f, 0.0f) + gameGridOffset,
			Size = new Vector2(34.0f, 34.0f),
			Origin = new Vector2(17.0f, 17.0f),
			IsVisible = true,
			IconId = 60934,
		};
		
		AddLaneNodeTimeline(laneImages[7], MathF.PI + MathF.PI / 4.0f);
        System.NativeController.AttachNode(laneImages[7], this);

        UpdateButtonStyle(config);
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var node in buttonImages) {
				node.Dispose();
			}

			foreach (var lane in laneImages) {
				lane.Dispose();
			}
			
			base.Dispose(disposing);
		}
	}

	public void SetActiveButtons(params int[]? indexes) {
		foreach (var image in buttonImages) {
			image.IsVisible = false;
		}
		
		if (indexes is null) return;
		
		foreach (var index in indexes) {
			buttonImages[index].IsVisible = true;
		}
	}

	public void SetActiveLanes(params int[]? indexes) {
		foreach (var lane in laneImages) {
			lane.IsVisible = false;
		}
		
		if (indexes is null) return;

		foreach (var index in indexes) {
			laneImages[index].IsVisible = true;
		}
	}

	private void AddLaneNodeTimeline(ImageNode imageNode, float rotation) {
		imageNode.AddTimeline(new TimelineBuilder()
			.BeginFrameSet(1, 120)
			.AddFrame(1, scale: new Vector2(1.4f, 1.4f), rotation: rotation)
			.AddFrame(60, scale: new Vector2(1.0f, 1.0f), rotation: rotation)
			.AddFrame(120, scale: new Vector2(1.4f, 1.4f), rotation: rotation)
			.EndFrameSet()
			.BeginFrameSet(121, 130)
			.AddFrame(121, scale: new Vector2(1.0f, 1.0f), rotation: rotation)
			.EndFrameSet()
			.Build());
	}

    public void UpdateButtonStyle(MiniCactpotHelperConfig config) {
        foreach (var image in buttonImages) {
            image.IconId = config.IconId;
			image.Color = config.ButtonColor;
        }
        
        foreach (var image in laneImages) {
            image.Color = config.LaneColor;
        }
        
		Timeline?.PlayAnimation(config.EnableAnimations ? 200 : 201);
    }
}
