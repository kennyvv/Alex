﻿using System;
using Microsoft.Xna.Framework;

namespace Alex.API.Utils
{
	public class PlayerLocation : ICloneable
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }

		public float Yaw { get; set; }
		public float Pitch { get; set; }
		public float HeadYaw { get; set; }
		public bool OnGround { get; set; }

		public PlayerLocation()
		{
		}

		public PlayerLocation(float x, float y, float z, float headYaw = 0f, float yaw = 0f, float pitch = 0f)
		{
			X = x;
			Y = y;
			Z = z;
			HeadYaw = headYaw;
			Yaw = yaw;
			Pitch = pitch;
		}

		public PlayerLocation(double x, double y, double z, float headYaw = 0f, float yaw = 0f, float pitch = 0f) : this((float)x, (float)y, (float)z, headYaw, yaw, pitch)
		{
		}

		public PlayerLocation(Vector3 vector, float headYaw = 0f, float yaw = 0f, float pitch = 0f) : this(vector.X, vector.Y, vector.Z, headYaw, yaw, pitch)
		{
		}

		/*public PlayerLocation(MiNET.Utils.PlayerLocation p)
		{
			if (p == null) return;
			X = p.X;
			Y = p.Y;
			Z = p.Z;

			Yaw = p.Yaw;
			HeadYaw = p.HeadYaw;
			Pitch = p.Pitch;
		}*/

		public BlockCoordinates GetCoordinates3D()
		{
			return new BlockCoordinates((int)X, (int)Y, (int)Z);
		}

		public double DistanceTo(PlayerLocation other)
		{
			return Math.Sqrt(Square(other.X - X) +
							 Square(other.Y - Y) +
							 Square(other.Z - Z));
		}

		public double Distance(PlayerLocation other)
		{
			return Square(other.X - X) + Square(other.Y - Y) + Square(other.Z - Z);
		}

		private double Square(double num)
		{
			return num * num;
		}

		public Vector3 ToVector3()
		{
			return new Vector3(X, Y, Z);
		}
		/*
		public Vector3 ToRotationVector3(bool withPitch = false)
		{
			return new Vector3(withPitch ? Pitch : 0f, HeadYaw, 0f);
		}

		public Vector3 GetDirection()
		{
			Vector3 vector = new Vector3();

			double pitch = Pitch.ToRadians();
			double yaw = Yaw.ToRadians();
			vector.X = (float)(-Math.Sin(yaw) * Math.Cos(pitch));
			vector.Y = (float)-Math.Sin(pitch);
			vector.Z = (float)(Math.Cos(yaw) * Math.Cos(pitch));

			return vector;
		}

		public Vector3 GetHeadDirection()
		{
			Vector3 vector = new Vector3();

			double pitch = Pitch.ToRadians();
			double yaw = HeadYaw.ToRadians();
			vector.X = (float)(-Math.Sin(yaw) * Math.Cos(pitch));
			vector.Y = (float)-Math.Sin(pitch);
			vector.Z = (float)(Math.Cos(yaw) * Math.Cos(pitch));

			return vector;
		}
*/
		public static PlayerLocation operator *(PlayerLocation a, float b)
		{
			return new PlayerLocation(
				a.X * b,
				a.Y * b,
				a.Z * b,
				a.HeadYaw * b,
				a.Yaw * b,
				a.Pitch * b);
		}
		
		public static PlayerLocation operator +(PlayerLocation a, Vector3 b)
		{
			var (x, y, z) = b;

			return new PlayerLocation(
				a.X + x,
				a.Y + y,
				a.Z + z,
				a.HeadYaw,
				a.Yaw,
				a.Pitch)
			{
				OnGround = a.OnGround
			};
		}

		public static implicit operator Vector2(PlayerLocation a)
		{
			return new Vector2(a.X, a.Z);
		}

		public static implicit operator Vector3(PlayerLocation a)
		{
			return new Vector3(a.X, a.Y, a.Z);
		}

		public static implicit operator PlayerLocation(BlockCoordinates v)
		{
			return new PlayerLocation(v.X, v.Y, v.Z);
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		public override string ToString()
		{
			return $"X={X}, Y={Y}, Z={Z}, HeadYaw={HeadYaw}, Yaw={Yaw}, Pitch={Pitch}";
		}

		//public PlayerLocation Clone()
	//	{
			
		//}
		
	/*	public Vector3 PreviewMove(Vector3 moveVector)
		{
			return ToVector3() + moveVector; Vector3.Transform(moveVector,
				       Matrix.CreateRotationY(-MathHelper.ToRadians(HeadYaw)));
		}

		public void Move(Vector3 moveVector)
		{
			//var headDirection = GetHeadDirection();
			var preview = PreviewMove(moveVector);
			X = preview.X;
			Y = preview.Y;
			Z = preview.Z;
		}*/
	}
}
